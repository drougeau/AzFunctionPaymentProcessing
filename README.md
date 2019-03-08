# Azure Function Payment Processing Sample

Sample Azure Durable Function for Payment Processing of a CSV file with API Secret stored in Azure Key Vault

### Prerequisites:
- Azure Storage Account
- Azure Key Vault
- Azure Function (Can be create when Publishing from Visual Studio)

### Diagram:
![GitHub Logo](/workflow.png)

### Workflow:
1. This sample monitor an Azure Blob storage container "inputfiles" for new Blob
1. Once a file is drop in the contaner, the Azure Function will be triggerred automatically, open and read the file content
1. Initiate a Processing Function for each Transaction (Line) (Parallel Processing),
1. Then save all processed transactions into an "outputfiles" container

### Setup:
- Clone the repository
- Open the project in Visual Studio 2017
- Modify the following entries in each of the source files
  - <STORAGE_ACCOUNT_NAME>
  - <STORAGE_KEY>
- Configure Azure Key Vault Secret (https://medium.com/statuscode/getting-key-vault-secrets-in-azure-functions-37620fd20a0b)
- Modify the following entries in each of the host.json file
  - <KEYVAULT_ACCOUNTNAME>
  - <KEYVAULT_SECRETNAME>
  - <KEYVAULT_SECRETVERSION>
- Build the project
- [Optional] You can run the project locally prior to Publish to azure (Without KeyVault)
- Publish to Azure Function
- Apply Azure Key Vault Permissions for the Azure Function to be able to read the Secret 


#### VS2017 Publish Settings Sample Screenshot:
![GitHub Logo](/PublishProfile-AKVSettings.PNG)


#### Note:
Source file also includes a working httptrigger which has been commented out
