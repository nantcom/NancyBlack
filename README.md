
<a rel="license" href="http://creativecommons.org/licenses/by/4.0/"><img alt="Creative Commons License" style="border-width:0" src="https://i.creativecommons.org/l/by/4.0/88x31.png" /></a><br />This work by <a xmlns:cc="http://creativecommons.org/ns#" href="https://github.com/nantcom/NancyBlack/" property="cc:attributionName" rel="cc:attributionURL">NantCom Co., Ltd.</a> is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by/4.0/">Creative Commons Attribution 4.0 International License</a>.<br />Based on a work at <a xmlns:dct="http://purl.org/dc/terms/" href="https://github.com/nantcom/NancyBlack/" rel="dct:source">https://github.com/nantcom/NancyBlack/</a>.
=======
# NancyBlack
A website system built on top of NancyFX. The goal is to create an reusable website backend which will allow us to easily create new website faster and easier with one shared code base. It is very useful for us, and to ensure that customers can continue to support their websites if we someday no longer has a team - we decided to put it on Github.

The system is currently under active development and is being used by our company's website such as http://www.level51pc.com,
http://www.gohub.biz and few other sites we built for customers.

## Main Design Goals
We want the system to be simple to use, and simple to create new website with it. So we design NancyBlack with these goals in mind:

- Easy for our developers to create new website : all of the frequently used JavaScript frameworks are included with the project, everything is laid out and ready to use.

- NoSQL Style Database with built-in API : if there is a new form on the web, we should be able to just send the data in the form to server and save it to database without having to create any C# class, create new GET/POST/PATCH/DELETE API or even define the table on the database.

- Easy for site owner to modify their website : this will help reduce support calls!

- Copy-and-Paste Deployment : we want server code/data and local code/data to be 1:1 to reduce deployment risk and easy for debugging the problem with live website.

- Easy for site admin to maintain : we want the website to backup itself and can be managed remotely without remote desktop access. 

## Current Status
We already using this system in many of our production projects. Only part that was not developed is the tool to restore website database from backup.

## Licensing
MS-PL
https://opensource.org/licenses/MS-PL

