# Gust

## This project: 
* Provides metadata about an entity framework model in a schame that is compatible with the breeze js 
client
* Provides an API for saving a collection of entities that might be co-dependent and therefore might:
 - need to be saved in a specfic order
 - need to have their foreign keys updated after another entity is saved
 - After the save the api returns a description of the results letting the client know what are the
   current values the entities hold (post save) and how their keys might have changed. 
   this result is also compatible with the results
   the server breeze package returns