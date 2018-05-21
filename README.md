# STUN Server
Docker
```
docker pull hmbsbige/stunserver
docker run -d --restart=always --net=host -p 3478:3478/tcp -p 3478:3478/udp --name=stunserver hmbsbige/stunserver --mode full --primaryinterface $IP1 --altinterface $IP2
```

# STUN Client Preview
![](pic/1.png)