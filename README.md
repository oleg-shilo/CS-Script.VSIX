# CS-Script.VSIX

Visual Studio 2017 CS-Script Tools extension
_CS-Script Tools extension allows managing C# scripts with Visual Studio 2017._

## CS-Script resources:
- Summary: https://www.cs-script.net/  
- Installation: https://chocolatey.org/packages/cs-script
- GitHub: https://github.com/oleg-shilo/cs-script

## CS-Script Tools Tutorial

* _**Installing CS-Script Tools**_

  Install CS-Script.VSIX from Visual Studio extension manager or from Visual Studio Marketplace: https://marketplace.visualstudio.com/items?itemName=OlegShilo.CS-ScriptToolsVS2017 
  <br/>

* _**Opening CS-Script Tools window**_
  In Visual Studio select `View->Other Windows->CS-Script Tools` menu:<br/>
  ![](images/vsx_menu.png)
  <br/>

* _**Creating new script**_

  Click "New Script" command:<br/>
  ![](images/create_script.png)
  <br/>

* _**Opening existing script**_

  - With Open File dialog

    1. Click "Open Script" command:<br/>
       ![](images/open_script.png)
    2. The Open File dialog will popup. Navigate and select the script file. The script file will be loaded into Visual Studio.
    <br/>

  - By Drag-n-drop

    1. Drag and drop the script file anywhere in the CS-Script Tools window.<br/>
       ![](images/drag_n_drop.png)
    2. The script file will be loaded into Visual Studio.
    <br/>

* _**Refreshing opened script references**_

  At any time you can trigger parsing CS-Script directives (//css_*) in order to update set of imported scripts and referenced assemblies. The following is a typical scenario for including spike.cs script and adding reference to System.Xml.dll assembly to the new script.cs script.

  - Create a new script with the "New Script" command.
  - Insert //css_include and  //css_reference directives at the top of the script:<br/>
    ![](images/vsx_refresh1.png)
  - Click on "Refresh Script Project" command:<br/>
    ![](images/refresh_script.png)
  - The script project will be updated with one new source file and new referenced assembly:<br/>
    ![](images/vsx_refresh2.png)
    <br/>

* _**Opening script from the Recent Scripts list**_

  The appearance and the implementation of the "Recent Scripts" is very similar to the "Recent Projects" list on the "Start page" of Visual Studio. This also includes support for context menus for  script items  for  "Open Script", "Open Script Folder" and "Remove From List". This also includes "pinning" and "unpinning" items.

  You can edit "Recent Scripts" content by executing "Manage Recent Scripts" command:<br/>
  ![](images/vsx_recent1.png)

  Click the script item in the "Recent Scripts" list to open it in Visual Studio.<br/>
  ![](images/vsx_recent.png)
  <br/>

* _**Executing CS-Script commands**_
  Click any of link button in the commands pane. The buttons have self explanatory captions:<br/>
  ![](images/vsx_commands.png)
