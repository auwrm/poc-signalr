# POC Azure SignalR Service (stateless)

# Prerequisites
1. [Azure Storage Emulator (Azurite)](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio)
1. [Azure SignalR Local Emulator](https://github.com/Azure/azure-signalr/blob/dev/docs/emulator.md)
1. Docker compose MongoDb
1. optional → [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest)
1. optional → [Azure Storage Explorer](https://azure.microsoft.com/en-us/products/storage/storage-explorer/)
1. optional → [Azure Function Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Ccsharp%2Cportal%2Cbash#install-the-azure-functions-core-tools)


# How to run
1. ติดตั้งโปรแกรม `Azure Storage Emulator (Azurite)` และ `Azure SignalR Local Emulator` และทำการ Run ให้เรียบร้อย (ขั้นตอนต่างๆอยู่ด้านล่าง)
1. เข้าไปในโฟเดอร์ `poc.msgcenter` แล้วเปลี่ยนชื่อไฟล์ `local.settings.sample.json` เป็น `local.settings.json`
1. เปิด Project Solution แล้วคลิกขวาที่โปรเจค `msgcenter` แล้วเลือก `Manage User Secrets`
1. นำ ConnectionString ที่ได้จากการเปิด SignalR Emulator มาใส่ใน Secret ตามตัวอย่างด้านล่าง
1. ทำการ Run (CTRL + F5) แล้วเข้าหน้าเว็บด้วย URL `http://localhost:7071/api/index`

User Secrets
```
{
  "AzureSignalRConnectionString": "Endpoint=http://localhost;Port=8888;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ABCDEFGH;Version=1.0;"
}
```

## Azure Storage Emulator (Azurite)
1. เข้าไปตาม path ด้านล่างเพื่อเปิดโปรแกรม `azurite.exe` ด้วย **Administrator mode** (หากหาไม่เจอให้เปิด Visual Studio Installer > modify แล้วเพิ่ม Azure Development)
```
C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\Microsoft\Azure Storage Emulator
```

2. Default ConnectionString
```
DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;
```

## Azure SignalR Local Emulator
1. ติดตั้ง SignalR Emulator
```
dotnet tool install  -g Microsoft.Azure.SignalR.Emulator
dotnet tool update -g Microsoft.Azure.SignalR.Emulator
```

2. เปิด command prompt เข้าไปที่โฟเดอร์ `poc.msgcenter` แล้วใช้คำสั่งด้านล่างเพื่อเปิด SignalR Emulator
```
asrs-emulator start
```

3. (Optional) Set upstream
```
asrs-emulator upstream init
```