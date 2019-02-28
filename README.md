# Azure Function Payment Processing

Azure Durable Function Sample for Payment Processing of a CSV file

### Prerequisites:
- Azure Storage Account (Must Exist)
- Azure Function (Can be create whn Publishing from Visual Studio)
- Azure Key Vault (Optional - Not implemented yet)

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
- Build the project
- Publish to Azure Function
