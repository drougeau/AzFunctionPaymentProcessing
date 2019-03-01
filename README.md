# Azure Function Payment Processing Sample

Azure Durable Function Sample for Payment Processing of a CSV file with Secret

### Prerequisites:
- Azure Storage Account
- Azure Key Vault
- Azure Function (Can be create when Publishing from Visual Studio)

### Diagram:
![GitHub Logo](/workflow.png)

### Workflow:
1. This sample monitor an Azure Blob storage contaainer "Inputfiles" for new Blob
1. Once a file is drop in the contaner, The AzFunction will open and read the file content
1. Initiate a process for each Transaction (Line)
1. Then save all processed transactions into an "Outpufiles" container

### Setup:
- Clone the repository
- Open the Project in Visual Studio 2017
- Modify the following entries in each of the source files
  - <STORAGE_ACCOUNT_NAME>
  - <STORAGE_KEY>
- Configure Azure Key Vault (https://medium.com/statuscode/getting-key-vault-secrets-in-azure-functions-37620fd20a0b)
- Modify the following entries in each of the source files
  - <KEYVAULT_ACCOUNTNAME>
  - <KEYVAULT_SECRETNAME>
  - <KEYVAULT_SECRETVERSION>
- Build the project
- Publish to Azure Function
- Apply Azure Key Vault PErmissions for the Azure Function to read the Secret 


#### VS2017 Publish Settings Sample Screenshot:
![GitHub Logo](/PublishProfile-AKVSettings.PNG)
