## SERVER - CLIENT CONSOLE APP ##

THIS APP USES TCPLISTENER AND TCPCLIENT TO MANAGE CONNECTIONS AND SESSIONS.

THE WAY IT WORKS IT'S SIMPLE, SERVER CLASS MANAGES CONNECTIONS AND IS USED AS A "ADMIN", 
MEANING IT CAN SEND MESSAGE TO ALL CLIENTS, SEE
THEIR DATA (NAMES AND ENDPOINT), KICK CLIENTS, SEE ALL CLIENTS CONNECTED AND CLOSE SERVER.

CLIENT CLASS IS THE ONE THAT IS USED AS A WAY TO CONNECT TO SOMEONE 
USING SERVER CLASS, NEEDING IP ADDRESS, PORT AND A NAME THAT 
IS UNIQUE (WHILE A CLIENT CONNECTED IS USING A NAMED IT WON'T BE AVAILABLE
FOR ANYBODY ELSE, ONCE HE DISCONNECTS IT WILL BE AVAILABLE AGAIN).
CLIENTS CAN COMMUNICATE TO OTHER CLIENTS, ASK FOR CLIENTS LIST (ONLY NAMES WILL BE SENT)
AND EXIT THE EXECUTION.

EACH CLASS HAVE COMMANDS TO DO EVERYTHING MENTIONED ABOVE.
