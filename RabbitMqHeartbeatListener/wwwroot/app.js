const userTimezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
console.log(userTimezone); // Example: "Australia/Brisbane"


function getUserTimeZone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}