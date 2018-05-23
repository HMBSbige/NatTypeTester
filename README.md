# STUN Server
Docker
```
docker pull hmbsbige/stunserver
docker run -d --restart=always --net=host --name=stunserver hmbsbige/stunserver --mode full --primaryinterface $IP1 --altinterface $IP2
```

# STUN Client Preview
![](pic/1.png)