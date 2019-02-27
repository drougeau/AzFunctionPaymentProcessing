# AzFunctionPaymentProcessing

Azure Durable Function for Payment Processing

Prerequisites:
- Azure Storage Account
- Azure Function
- Azure KEy Vault (Optional - Not implemented yet)

![GitHub Logo](/workflow.png)

Workflow:
1- This sample monitor an Azure Blob storage contaainer "Inputfiles" for new Blob
2- Once a file is drop in the contaner, The AzFunction will open and read the file content
3- Initiate a process for each Transaction (Line)
4- Then save all processed transactions into an "Outpufiles" container

Setup:
- Clone the repository
- Modify the following entries in each of the source files
  - <STORAGE_ACCOUNT_NAME>
  - <STORAGE_KEY>
- Build the project
- Publish to azure Function
