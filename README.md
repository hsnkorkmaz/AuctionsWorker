# AuctionsWorker
Worker Application that inserts all Wow Auctions to MongoDB. Including all realms in EU and US.
Be carefull to use that with Azure Cosmos DB or some other cloud services because of the RU/s and datasize that needed. 
On average one execution will take minimum 500mb of space in db.

Application designed to execute every hour, so think twice to use that as a service.

If you are okay with the cost you can publish the application to C:\BlizzardService\AuctionsWorker and then you can use the below code in powershell to add as a service.

PowerShell Command
sc.exe create BlizzardAuction binpath= C:\BlizzardService\AuctionsWorker\AuctionsWorker.exe start= auto
