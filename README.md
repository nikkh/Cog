# Cog
This is an application that calls Azure Cognitive APIs to process audio files or text files and determine the sentiment.

It should be pretty easy to get going with this one.  Download the source (or just collaborate and improve it).  

You need to put your API keys for the Text Analytics and Bing Speech APIs in the app.config and also update configuration with folders where you want to drop incoming files, and where you want to archive to.

Then create m4a recordings with voice recorder or author text files and see what the sentiment is!

# Note

There is a batch size - the APIs are limited in the free version but you can send hundreds of documents per API Call.  if you increase the batch size the application will buffer your requests until it has a batch - therefore conserving your free API calls.

# Also Note

I have used chunking to send the audio files to the API.  If you have larger audio files you should use Web Sockets.  Keep you audio clips to around 10 seconds unless you want to modify the application for web sockets.
