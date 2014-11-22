This folder contains a scaffolded views for entities within this instance of Nancy Black.

The Admin Views are automatically generated when request is made to:
http://localhost:anyport/Admin/{table_name}
	- if table_name is never existed on the database, the view will not be generated
	- if table_name is already existed on the database, the view will be generated based on Last record found on the database