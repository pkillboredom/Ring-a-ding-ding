# Ring-a-ding-ding

This is a weird little Blazor Server app I made for a party to cause a certain sequence of events from my phone. Specifically, it will...

1. Kill a certain running fullscreen application.
2. Play a video in MPCHC as soon after as reasonably possible (the video began with a fake crash of the aforementioned application).
3. To control some hue lights at a key moment in said video.
4. To ring an attached bluetooth phone handset at a key moment in said video.

It did all of these things except step 4 correctly. Wasn't the code's fault though... weird bluetooth problems with my PC on the day of the party.

## Key Notes:

- This application is probably not generalizable to doing anything except the above.
- This application does what most developers may consider *bad things*. It launches and kills processes on the host from a web ui with no authentication. You shouldn't run it longer than you need to.
- This application has a really hacky logger -> signalR -> webui thing that I duct taped out of examples. Its bad and I have no idea what it does with multiple clients connected or anything like that.
- There's a half decent sample of how to adjust all the lights in a single hue room in C# in here. This took a lot more trial and error than it should have. That library really needs more (and updated) examples and xmldocs.
- There's also some nice magic regex for reading status from the MPCHC webui. Probably nothing of value beyond these last two bullet points.
