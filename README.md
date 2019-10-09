# NatTypeTester

- [x] RFC 3489
- [ ] RFC 5389

- [x] IPv4
- [ ] IPv6

## Preview
![](pic/1.png)

## STUN Server
### Docker
```
docker pull hmbsbige/stunserver
docker run -d --restart=always --net=host --name=stunserver hmbsbige/stunserver --mode full --primaryinterface $IP1 --altinterface $IP2
```
