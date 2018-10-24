# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - Unrelease
### Fixed
- Session has change `Dictionary<string, object>` so it can set any value type.
### Add
- SendRequest - Like HTTP send Request & recivice a Response
- Cookies - a `IDictionary<string, string>` can auto sync between two side <c>TcpJsonClient</c> instance.


## [1.0.0.3] - 2018-10-17
### Add
- SendBytes - Send a bytes block, Always receive a full block at a time

## [1.0.0.2] - 2018-10-15
### Add
- NamedStream - Multiplexing Current communication channels

## [1.0.0.1] - 2018-09-21
### Add
- XML Documentation Comments(zh-CN)

## [1.0.0] - 2018-09-20
### Add
- Implement Base Json Object Transfer Service
- Implement Single-string-represented command Transfer Service