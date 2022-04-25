# FAQ

## Url/Query String too long
if try login while debug in IIS Express on Windows, probably you will hit the **Url too long** 
or **Query String too long** error. This is a limitation of IIS Express, not limit of ASP.NET core. 
.Net(or .Net core) doesn't have those limits any more to support all OS. So there is no way
to setup the limits in .Net 5+ or .Net core. We cannot update web.config as most google search results 
suggest since we don't have this file in a .Net 5 project.

So how do we fix it in debug? Well we have to update the default IIS Express config on your dev Windows machine.
The schema file for IIS is named `IIS_schema.xml`. The IIS Express one locates at "%ProgramFiles(x86)%\IIS Express\config\schema" (%ProgramFiles% instead if you have a 64-bit server).
Open this file for edit and update `requestLimits.maxQueryString`, `requestLimits.maxUrl` to proper value.