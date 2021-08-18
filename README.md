# NatTypeTester
Channel | Status
-|-
CI | [![CI](https://github.com/HMBSbige/NatTypeTester/workflows/CI/badge.svg)](https://github.com/HMBSbige/NatTypeTester/actions)
Stun.Net | [![NuGet.org](https://img.shields.io/nuget/v/Stun.Net.svg?logo=nuget)](https://www.nuget.org/packages/Stun.Net/)

## RFC supports

- [x] [RFC 3489](https://datatracker.ietf.org/doc/html/rfc3489)
- [x] [RFC 5389](https://datatracker.ietf.org/doc/html/rfc5389)
- [x] [RFC 5769](https://datatracker.ietf.org/doc/html/rfc5769)
- [x] [RFC 5780](https://datatracker.ietf.org/doc/html/rfc5780)
- [ ] [RFC 7350](https://datatracker.ietf.org/doc/html/rfc7350)
- [ ] [RFC 7443](https://datatracker.ietf.org/doc/html/rfc7443)
- [ ] [RFC 7635](https://datatracker.ietf.org/doc/html/rfc7635)
- [ ] [RFC 8489](https://datatracker.ietf.org/doc/html/rfc8489)

## Internet Protocol

- [x] IPv4
- [x] IPv6

## Transmission Protocol

- [x] UDP
- [ ] TCP
- [ ] TLS-over-TCP
- [ ] DTLS-over-UDP

## Preview
![](pic/1.png)

## STUN Server
### Docker
```
docker pull hmbsbige/stunserver
docker run -d --restart=always --net=host --name=stunserver hmbsbige/stunserver --mode full --primaryinterface $IP1 --altinterface $IP2
```
