# Description

This repo contains a quick experiment to test some Azure Storage block blob functionality:

- Setting metadata on a new empty blob
- Adding and committing blocks
- Building up CSV data
- Listing blob metadata
- Downloading a blob as a CSV

It's a pretty hacky experiment!

Uses dotnet user-secrets or appsettings.json to store the account name of your storage account.

Use `dotnet user-secrets --project src/TestApp set StorageAccount <account name>` from the working directory root to set the account name in user secrets.

It also uses AzureCliCredential to authenticate to storage, so an `az login` to your azure account is needed.
