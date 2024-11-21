using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client;

namespace RabbitMqHeartbeatListener
{
    public class RabbitMqConnectionProvider(
        ILogger<RabbitMqConnectionProvider> logger, 
        IConnectionFactory connectionFactory
    ) : IDisposable
    {
        
        private IConnection? _connection;
        private bool isDisposed;
        private readonly object _connectionLock = new();


        private Task<IConnection>? _connectionTask;

        public async Task<IModel> CreateChannelAsync()
        {
            logger.LogDebug("Creating RabbitMQ channel.");

            var connection = await GetOrCreateConnectionAsync();

            var uid = Guid.NewGuid();

            var model = connection.CreateModel();

            if (model is null)
            {
                logger.LogWarning("Create Channel returned a null value.");
                throw new Exception("Create Channel returned a null value.");
            }

            logger.LogDebug("RabbitMQ channel created: {uid}", uid);

            model.ModelShutdown += (sender, e) => Model_ModelShutdown(e, uid);

            return model;
        }

        private void Model_ModelShutdown(ShutdownEventArgs e, Guid channelId)
        {
            var reason = e.ReplyText;
            logger.LogDebug("RabbitMQ channel shutdown {channelId}: {reason}", channelId, reason);
        }

        private async Task<IConnection> GetOrCreateConnectionAsync()
        {
            IConnection connection;
            lock (_connectionLock)
            {
                _connectionTask = GetOrStartConnectionTask();
            }

            connection = await _connectionTask.ConfigureAwait(false);

            if (connection is null)
            {
                throw new InvalidOperationException("Connection task completed but connection was null.");
            }

            return connection;
        }


        private async Task<IConnection> GetOrStartConnectionTask()
        {
            if (_connection is not null)
                return _connection;

            if (_connectionTask is not null)
                return await _connectionTask;

            return await Task.Run(async () =>
            {
                try
                {
                    var newConnection = CreateConnection();

                    newConnection.ConnectionShutdown += HandleConnectionShutdown;

                    newConnection.CallbackException += CallbackException;
                    newConnection.ConnectionBlocked += ConnectionBlocked;
                    newConnection.ConnectionUnblocked += ConnectionUnblocked;

                    lock (_connectionLock)
                    {
                        _connection = newConnection;
                    }

                    return newConnection;
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    lock (_connectionLock)
                    {
                        _connectionTask = null;
                    }
                }
            });
        }

        private void CallbackException(object? sender, CallbackExceptionEventArgs e)
        {
            logger.LogInformation($"RabbitMQ connection callback exception.");
        }

        private void ConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
        {
            logger.LogInformation("RabbitMQ connection blocked: {reason}", e.Reason);
        }

        private void ConnectionUnblocked(object? sender, EventArgs e)
        {
            logger.LogInformation($"RabbitMQ connection unblocked.");
        }

        private void HandleConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            var cause = e.ReplyText;
            logger.LogInformation("RabbitMQ connection shutdown. {cause}", cause);
        }

        private IConnection CreateConnection()
        {
            try
            {
                logger.LogInformation("Creating RabbitMQ connection.");

                var connection = connectionFactory.CreateConnection();

                logger.LogInformation("RabbitMQ connection created.");
                return connection;
            }
            catch (AuthenticationFailureException ex)
            {
                logger.LogError(ex, "RabbitMQ authentication failed: {errorMessage}", ex.Message);
                throw;
            }
            catch (PossibleAuthenticationFailureException ex)
            {
                logger.LogError(ex, "RabbitMQ connection failed due, possibly due to authentication: {errorMessage} ", ex.Message);
                throw;
            }
            catch (ProtocolVersionMismatchException ex)
            {
                logger.LogError(ex, "RabbitMQ connection failed due to protocol version mismatch: {errorMessage}", ex.Message);
                throw;
            }
            catch (BrokerUnreachableException ex)
                when (ex.InnerException is OperationInterruptedException e && e.ShutdownReason.ReplyCode == 530)
            {
                logger.LogError(ex, "RabbitMQ connection failed. User does not have permission to access this vhost.");
                throw;
            }
            catch (BrokerUnreachableException ex)
                when (ex.InnerException is OperationInterruptedException e && e.ShutdownReason.ReplyCode == 530)
            {
                logger.LogError(ex, "Brocker unreachable: {errorMessage}", ex.Message);
                throw;
                //await Task.Delay(500, stoppingToken).ConfigureAwait(false);
            }
            catch (ConnectFailureException ex)
            {
                logger.LogError(ex, "Connection failed");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create RabbitMQ connection: {errorMessage}", ex.Message);
                throw;
            }

        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;

            }

            isDisposed = true;
        }
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(isDisposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
