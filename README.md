
# Gust

## Origin story
Breeze: A collection if libraries, both for a client running JavaScript and for the server with the goal of giving you 
"Client-side querying, caching, dynamic object graphs, change tracking and notification, model validation..." (http://www.getbreezenow.com).

A custom CMS that I built for my company (https://www.languagezen.com) depends on breeze but sadly the server component does not support 
working with the new Entity Framework Core library and it is unclear if/when it will.

I created Gust as a replacement, it can produce meta data, data and update entities using the same format as breeze
for dotnet servers and it is being used by that the breeze.js client.

## Caveat Emptor

This is not a 1 to 1 replacement, but for us it does the job well. For example we do not use the OData interface
so it is not implemented.

I am in no way affiliated with breeze or with its owner company "ideablade".

## Future plans

If you report a bug I am very likely to fix it.

I am open to pull requests that add features you miss from breeze, but have no plans to add any myself at the moment. You should probably
talk to me early on to make sure we agree on the direction you are going with.

I hope one day to create a compatible replacement for the breeze frontend component that will:
* Maintain immutability, so it plays nicer with frameworks like React and "state containers" like Redux.
* Be easy to use with current tools I use like webpack and typescript


