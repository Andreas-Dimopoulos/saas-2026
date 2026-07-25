# TodoApi — httpie verification transcript

Every one of the twelve `/signup`, `/auth/*`, `/todos`, and `/todos/:id/items/:iid`
endpoints, run against a live instance of the app and captured verbatim (real commands,
real output — nothing here is invented). Run from `src/TodoApi` via
`dotnet run --launch-profile https`, against a freshly-migrated, empty database.

`UseHttpsRedirection` is active, so every request targets the HTTPS URL
(`https://localhost:7292`) directly. The dev cert isn't trusted by curl/httpie's default
CA bundle, so every call passes `--verify=no`.

httpie version used: `http --version` → `3.2.4`.

## 1. `POST /signup`

```
http --verify=no POST https://localhost:7292/signup email=alice@example.com password=Sup3rSecret1
```

```
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:44:51 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"id":1,"email":"alice@example.com","createdAt":"2026-07-25T12:44:51.9921288Z"}
```

## 2. `POST /auth/login`

```
http --verify=no POST https://localhost:7292/auth/login email=alice@example.com password=Sup3rSecret1
```

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:44:58 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"token":"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJhbGljZUBleGFtcGxlLmNvbSIsImp0aSI6IjhkNzQ4OGIxLThjNDUtNGYyNi05ZWNiLTg1YmZiMjIwZGI1ZSIsImV4cCI6MTc4NDk4NzA5OSwiaXNzIjoiVG9kb0FwaSIsImF1ZCI6IlRvZG9BcGkifQ.UbJ4jUQ_RF4aLbU64db0vIko8F0cUi38J2TFb14Y6lM","expiresAt":"2026-07-25T13:44:59.5000424Z"}
```

The token is captured into a shell variable for every subsequent call:

```
TOKEN=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJhbGljZUBleGFtcGxlLmNvbSIsImp0aSI6IjhkNzQ4OGIxLThjNDUtNGYyNi05ZWNiLTg1YmZiMjIwZGI1ZSIsImV4cCI6MTc4NDk4NzA5OSwiaXNzIjoiVG9kb0FwaSIsImF1ZCI6IlRvZG9BcGkifQ.UbJ4jUQ_RF4aLbU64db0vIko8F0cUi38J2TFb14Y6lM
```

## 3. `GET /todos` (empty)

```
http --verify=no GET https://localhost:7292/todos "Authorization:Bearer $TOKEN"
```

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:45:21 GMT
Server: Kestrel
Transfer-Encoding: chunked

[]
```

## 4. `POST /todos`

```
http --verify=no POST https://localhost:7292/todos "Authorization:Bearer $TOKEN" title="Groceries"
```

```
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:45:29 GMT
Server: Kestrel
Location: https://localhost:7292/todos/1
Transfer-Encoding: chunked

{"id":1,"title":"Groceries","createdBy":"alice@example.com","createdAt":"2026-07-25T12:45:30.1065265Z","updatedAt":"2026-07-25T12:45:30.1065265Z","items":[]}
```

Todo id `1` from here on.

## 5. `GET /todos/:id`

```
http --verify=no GET https://localhost:7292/todos/1 "Authorization:Bearer $TOKEN"
```

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:45:36 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"id":1,"title":"Groceries","createdBy":"alice@example.com","createdAt":"2026-07-25T12:45:30.1065265","updatedAt":"2026-07-25T12:45:30.1065265","items":[]}
```

## 6. `PUT /todos/:id`

```
http --verify=no PUT https://localhost:7292/todos/1 "Authorization:Bearer $TOKEN" title="Groceries v2"
```

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:45:44 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"id":1,"title":"Groceries v2","createdBy":"alice@example.com","createdAt":"2026-07-25T12:45:30.1065265","updatedAt":"2026-07-25T12:45:44.886985","items":[]}
```

## 7. `POST /todos/:id/items`

```
http --verify=no POST https://localhost:7292/todos/1/items "Authorization:Bearer $TOKEN" name="Milk"
```

```
HTTP/1.1 201 Created
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:45:52 GMT
Server: Kestrel
Location: https://localhost:7292/todos/1/items/1
Transfer-Encoding: chunked

{"id":1,"name":"Milk","done":false,"createdAt":"2026-07-25T12:45:52.2905342Z","updatedAt":"2026-07-25T12:45:52.2905342Z"}
```

Item id `1` from here on.

## 8. `GET /todos/:id/items/:iid`

```
http --verify=no GET https://localhost:7292/todos/1/items/1 "Authorization:Bearer $TOKEN"
```

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:45:58 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"id":1,"name":"Milk","done":false,"createdAt":"2026-07-25T12:45:52.2905342","updatedAt":"2026-07-25T12:45:52.2905342"}
```

## 9. `PUT /todos/:id/items/:iid`

```
http --verify=no PUT https://localhost:7292/todos/1/items/1 "Authorization:Bearer $TOKEN" name="Oat milk" done:=true
```

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Sat, 25 Jul 2026 12:46:05 GMT
Server: Kestrel
Transfer-Encoding: chunked

{"id":1,"name":"Oat milk","done":true,"createdAt":"2026-07-25T12:45:52.2905342","updatedAt":"2026-07-25T12:46:05.7542941Z"}
```

## 10. `DELETE /todos/:id/items/:iid`

```
http --verify=no DELETE https://localhost:7292/todos/1/items/1 "Authorization:Bearer $TOKEN"
```

```
HTTP/1.1 204 No Content
Date: Sat, 25 Jul 2026 12:46:12 GMT
Server: Kestrel
```

## 11. `DELETE /todos/:id`

```
http --verify=no DELETE https://localhost:7292/todos/1 "Authorization:Bearer $TOKEN"
```

```
HTTP/1.1 204 No Content
Date: Sat, 25 Jul 2026 12:46:21 GMT
Server: Kestrel
```

## 12. `GET /auth/logout`

```
http --verify=no GET https://localhost:7292/auth/logout "Authorization:Bearer $TOKEN"
```

```
HTTP/1.1 204 No Content
Date: Sat, 25 Jul 2026 12:46:29 GMT
Server: Kestrel
```

## Proof: the revoked token is now rejected

Same token, one more request, no logout in between — this is the whole point of the
jti-denylist design: the token is still cryptographically valid and unexpired, but it's
been revoked, so it must be rejected.

```
http --verify=no GET https://localhost:7292/todos "Authorization:Bearer $TOKEN"
```

```
HTTP/1.1 401 Unauthorized
Content-Length: 0
Date: Sat, 25 Jul 2026 12:46:36 GMT
Server: Kestrel
WWW-Authenticate: Bearer error="invalid_token"
```
