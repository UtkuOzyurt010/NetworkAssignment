In 2 split terminal do:

cd server
dotnet run

and 

cd client
dotnet run

We decided to handle incorrect DNSLookups by continuing the next queued DNSLookups so client has a complete overview of whats missing,
 but NOT sending an END message back to the client.