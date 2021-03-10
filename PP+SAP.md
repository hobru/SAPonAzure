# SAP on Azure - Power Platform & SAP Webinar Deep-Dive
Content related to the unofficial SAP on Azure YouTube Channel, http://youtube.com/SAPonAzure

Audio-Podcast available via https://anchor.fm/saponazure 

---
This section focuses on the **"Extending SAP solutions using Microsoft Power Platform"** webinar deep-dive series and provides the used code-snippets to make it easier to follow the required steps. 

---

## Power Platform + SAP (1/10): Preparing the required services & tools
Link to YouTube: [Preparing the required services & tools](https://youtu.be/pUFqDueKHso)

SAP on Azure - Based on the "Extending SAP solutions using Microsoft Power Platform" webinar this video series shows the required steps to get the 4 demos working in your own environment. All the required tools and resources are available as trial versions. 

In this video we show how to get started and prepare all the services and tools that are required to create the demos. 

* 0:00 Intro
* 0:48 Webinar
* 3:42 SAP Gateway Demo System
* 6:47 SAP Fiori & OData service
* 8:36 Microsoft 365 Developer Program
* 9:58 Power Platform

[SAP Webinar | Microsoft Power Apps](https://info.microsoft.com/ww-Landing-Extend-SAP-solutions-using-Microsoft-Power-Platform.html?LCID=EN-US)

[PowerPlatformConnectors](https://github.com/microsoft/PowerPlatformConnectors/tree/dev/custom-connectors/SAP-ODATA-Demo) 

[Power Apps - Business Apps | Microsoft Power Apps](https://powerapps.microsoft.com/en-us/)

[Create an Account on the SAP Gateway Demo System](https://developers.sap.com/tutorials/gateway-demo-signup.html)

---
## Power Platform + SAP (2/10): Creating Power App and using Custom Connector to connect to SAP
Link to YouTube: [Preparing the required services & tools](https://youtu.be/MJDAAedmvsw)

In this video we use the SAP Customer Connector available on GitHub to create a simple Power App and read product information from the SAP Gateway Demo System

* 0:00 Intro
* 0:47 GitHub Customer Connector
* 1:41 Power Apps - Custom Connector
* 5:47 Importing Power Apps


[SAP ODATA [Sample] Connector](https://github.com/microsoft/PowerPlatformConnectors/tree/master/custom-connectors/SAP-ODATA-Demo)

[Azure Trial Subscription](http://azure.com/free)



---
## Power Platform + SAP (3/10): Update SAP products from Power App using Power Automate
Link to YouTube: [Preparing the required services & tools](https://youtu.be/G31mHBE7Xx0)

In this video we leverage the Customer Connector to update products in the SAP system. 

* 0:00 Intro
* 0:48 Updating SAP data via Power Automate
* 1:51 Power Apps - Prepare to update
* 3:20 Power Automate 
* 5:02 Add Product Update Action
* 8:49 Using the Update Flow in Power Apps
* 10:58 Testing the update process

From within Power App, call the Power Automate flow via: 
``` 
UpdateODataEPM.Run(ProductBox.Product.ProductID; ProductBox.Product.__metadata.etag; ProductNameTB.Text; DescriptionTB.Text; PriceTB.Text) 
``` 

In the Power Automate Flow you need to handle the cookie from the previous call to the SAP sytem 
```
replace(outputs('Get_product')['headers']['Set-Cookie'],',',';')
```

[SAP ODATA [Sample] Connector](https://github.com/microsoft/PowerPlatformConnectors/tree/master/custom-connectors/SAP-ODATA-Demo)


---
## Power Platform + SAP (4/10): Using AI Builder to extract information from PDF documents in Power Automate
Link to YouTube: [Preparing the required services & tools](https://youtu.be/v94BDuhp6co)

In this video we use the Forms Reconizer as part of the AI Builder to extract relevant information from an incoming PDF document via Email. 

* 0:00 Intro
* 1:04 PDF Processing
* 1:30 Full Power Automate Flow
* 2:21 AI Builder 
* 5:12 Train Models
* 7:40 Create Power Automate Flow
* 8:44 Use AI Builder Action
* 10:07 Test the flow

---
## Power Platform + SAP (5/10): Creating a product in the SAP system
Link to YouTube: [Preparing the required services & tools](https://youtu.be/33cj73aDBtQ)

In this video we use the results from the AI Builder and create a product in the SAP system 

* 0:00 Intro
* 0:47 PDF Processing
* 1:11 Full Power Automate Flow
* 2:30 Adding SAP Custom Connector Action 
* 7:49 Add send Email step
* 8:44 Test the flow
* 11:00 Check SAP System

For the hard-coded values that were used you can use:
 
|Property|Value|
| -------------- | :--------- | 
|x-csrf-token| Token from prevoius step|
|x-ms-cookie-header|replace(outputs('Get_product')['headers']['Set-Cookie'],',',';')|
|Product ID|@items('Apply_to_each_2')?['ProductID/value']|
|Type Code|Product|
|Category|Notebooks|
|Supplier ID|0100000056|
|Product Tax Code|1|
|Currency Code|(Internal) United States Dollar (5 Dec.)|
|Unit of Measure|each|
|Weight unit|Kilogram|
|Dimension unit|Centimeter|

---
## Power Platform + SAP (6/10): Sending Adaptive Cards to Teams for Approval
Link to YouTube: [Preparing the required services & tools](https://youtu.be/gZmHaaq3aow)

In this video we insert an approval step in the flow, to present a user with an Adaptive Card and ask for a decision whether the product should be created in the SAP system. 

* 0:00 Intro
* 0:47 Full Power Automate Flow
* 1:37 Adaptive Cards
* 2:44 Adding Teams Action 
* 3:20 Adaptive Cards Designer
* 5:27 Test the flow
* 7:03 Adding Conditions
* 8:23 Testing again

```
{
    "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
    "type": "AdaptiveCard",
    "version": "1.2",
    "body": [
        {
            "type": "TextBlock",
            "weight": "Bolder",
            "size": "Medium",
            "text": "Add new product request"
        },
        {
            "type": "TextBlock",
            "text": "Please check the product details and approve / reject",
            "isSubtle": true,
            "wrap": true
        },
        {
            "type": "TextBlock",
            "wrap": true,
            "$data": "@{items('Apply_to_each_2')?['ProductID/value']}",
            "text": "@{items('Apply_to_each_2')?['ProductID/value']}"
        },
        {
            "type": "TextBlock",
            "wrap": true,
            "text": "@{items('Apply_to_each_2')?['Name/value']}",
            "$data": "@{items('Apply_to_each_2')?['Name/value']}"
        },
        {
            "type": "TextBlock",
            "text": "@{items('Apply_to_each_2')?['Price/value']}",
            "$data": "@{items('Apply_to_each_2')?['Price/value']}"
        }
    ],
    "actions": [
        {
            "type": "Action.Submit",
            "title": "Approve"
        },
        {
            "title": "Reject",
            "type": "Action.Submit"
        }
    ]
}
```

Retrieve the decision from the Adaptive Card
```
body('Post_an_Adaptive_Card_to_a_Teams_user_and_wait_for_a_response')?['submitActionId']
```


[Adaptive Cards Designer]( https://adaptivecards.io/designer )


---
## Power Platform + SAP (7/10): Creating a Chatbot in Teams to access data from SAP
Link to YouTube: [Preparing the required services & tools](https://youtu.be/dbzW2aSuZnw)

In this video we use Power Virtal Agent to create a Chatbot in Teams which leverages the previous Custom Connector to retrieve live data from the SAP System. 

* 0:00 Intro
* 0:57 Power Virtual Agents
* 1:57 Starting PVA from Teams
* 2:25 Creating a Bot 
* 3:47 Greeting Topic
* 5:47 Creating SAP Flow
* 6:45 Using SAP Custom Connector
* 11:16 Testing the bot
* 12:25 Publishing and test


---
## Power Platform + SAP (8/10): Adding QnA Maker features in a Power App
Link to YouTube: [Preparing the required services & tools](https://youtu.be/FJc3bjymI54)

In this video we use the QnA Maker portal to create a bot based on an existing FAQ page and then integrate this bot within a new Power App. 

* 0:00 Intro
* 1:08 Qna Maker
* 1:50 Integrating QnAMaker 
* 3:48 Starting QnAMaker 
* 5:50 Create QnAMaker Service
* 7:26 Creating Knowledge Base
* 8:43 Publish the service
* 9:20 Create a new Power App
* 10:43 Create Chat-Collection
* 15:06 Add QnAMaker Connector
* 17:20 Update Images
* 18:34 First Test


Setting up the Collection when starting the Power App enables us to retrieve and list the questions and answers from the QnA Maker.
```
ClearCollect(chat, {id:0,name:"Alex",text:"Welcome "&Left(User().FullName,Find(" ",User().FullName))&"! How can I help you today?."});
```

Once setup, the Collection can be updated with the actual questions & answers via:  
```
Collect(chat; {id: 1; name: User().FullName; text: startChatTxt.Text});;
Collect(chat; {id: 0; name: "Contoso Bot"; text: First(QnAMaker.GenerateAnswer("53068458-56bf-4ccc-aaaa-573eac18883e";"https://qnamaker-powerappsdemo.azurewebsites.net/qnamaker"; "285583c0-ef36-4050-8a60-90e173ec32b2";startChatTxt.Text).answers).answer});;
```

Displaying and switching the picture from the User image to the Bot images
```
If(ThisItem.id=1; User().Image; SAP_on_Azure_Logo)
```


[QnAMaker Portal](qnamaker.ai)

[Azure Trial Subscription](http://azure.com/free)


---
## Power Platform + SAP (9/10): Adding SharePoint List in a Power App
Link to YouTube: [Preparing the required services & tools](https://youtu.be/9mANGc93LOc)

In this video we use the SharePoint connector to show a SharePoint list in the Power Apps with only a few clicks. Then we filter this list and only show documents that have a certain attribute associated with it. 

* 0:00 Intro
* 0:58 Power Platform Connectors
* 1:36 SharePoint Site 
* 2:24 SharePoint Connector 
* 4:03 Sort and Filter

```
Sort(Filter('Product Documents', SoldToHbr.Email=User().Email), Name, Ascending)
```

---
## Power Platform + SAP (10/10): Coming Soon
