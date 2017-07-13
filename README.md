# AmsHLSProxy
A web proxy for making Token Authroized AES encrypted HLS stream output from Azure Media Services to work in Safari on both macOS and iOS systems.


This code repository is implemented based on the idea and implemention by [Mingfei Yan](https://azure.microsoft.com/en-us/blog/how-to-make-token-authorized-aes-encrypted-hls-stream-working-in-safari/), please access the original code repository at https://github.com/AzureMediaServicesSamples/HLSSafariProxy

It seems the code in original repo is out of maintaince, and there is some small issues in the code. Additionally, I want to make this pure "functional" API to be a real [Azure Function](https://azure.microsoft.com/en-us/services/functions/). 

Now, it's online and availabe to you to test your media now: https://amshlsproxy.azurewebsites.net/api/Manifest
Just put your stream url and token in URI encoded format into following URL pattern, then you get a final URL that is playable for Safari browsers and iOS builtin AVPlayer control:

https://amshlsproxy.azurewebsites.net/api/Manifest?playbackUrl={streamUrl}&token={token}

For instance, the result could be: https://amshlsproxy.azurewebsites.net/api/Manifest?playbackUrl=https%3A%2F%2Fmyvideo.streaming.mediaservices.windows.net%2Fea2bc2d6-b40a-4587-a528-91c609d891df%2FUSCapitalGainsTax.ism%2Fmanifest(format%3Dm3u8-aapl)&token=Bearer%20eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJ1cm46bWljcm9zb2Z0OmF6dXJlOm1lZGlhc2VydmljZXM6Y29udGVudGtleWlkZW50aWZpZXIiOiJmZmZhYTgxYy1lMDM1LTQyZ


Note: Pleasse DO NOT use this function as production use, I've set a quota limitation in case of unexpected cost. :-) 
