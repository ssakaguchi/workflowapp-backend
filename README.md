# Workflow App Backend

WorkflowApp.Api は、業務ワークフローにおける「申請」機能を想定した ASP.NET Core Web API です。

ユーザー登録・ログインによる JWT 認証を行い、認証済みユーザーごとに申請データの作成・取得・更新・削除・ステータス更新を行うことができます。

## 作成背景

デスクトップアプリケーション中心の開発経験から、Web アプリケーション開発へのスキル拡張を目的として作成したポートフォリオです。

特に以下の技術・設計の習得を目的としています。

- ASP.NET Core Web API による REST API 開発
- Entity Framework Core を利用したデータアクセス
- xUnit を利用した単体テスト・API テスト

## 主な機能

### 認証機能

- ユーザー登録
- ログイン
- JWT トークン発行
- 認証済みユーザー情報の取得
- パスワードハッシュ化
- ログインIDの一意制約

### 申請機能

- 申請の作成
- 申請一覧の取得
- 申請一覧のページネーション
- 申請一覧のステータス絞り込み
- 申請詳細の取得
- 申請の更新
- 申請の削除
- 申請ステータスの更新
- 認証ユーザーごとの申請データ分離
- 入力値バリデーション

## 技術スタック

| 分類           | 使用技術                                                                             |
| -------------- | ------------------------------------------------------------------------------------ |
| 言語           | C#                                                                                   |
| フレームワーク | ASP.NET Core Web API                                                                 |
| .NET           | .NET 8                                                                               |
| ORM            | Entity Framework Core                                                                |
| DB             | SQLite                                                                               |
| 認証           | JWT Bearer Authentication                                                            |
| API確認        | Swagger / Swagger UI                                                                 |
| テスト         | xUnit                                                                                |
| テスト補助     | FluentAssertions / NSubstitute / Microsoft.AspNetCore.Mvc.Testing / EF Core InMemory |
| 開発環境       | Visual Studio / Visual Studio Code                                                   |

## アーキテクチャ

責務を分離するため、Controller / Service / DTO / Domain / Infrastructure に分けた構成にしています。

```text
WorkflowApp.Api
├── WorkflowApp.Api
│   ├── Controllers
│   │   ├── AuthController.cs
│   │   ├── ApplicationsController.cs
│   │   └── SampleController.cs
│   ├── Domain
│   │   ├── Entities
│   │   │   ├── Application.cs
│   │   │   └── User.cs
│   │   └── Enums
│   │       └── WorkflowStatus.cs
│   ├── DTOs
│   │   ├── Auth
│   │   └── Applications
│   ├── Infrastructure
│   │   ├── Data
│   │   │   └── AppDbContext.cs
│   │   └── Security
│   │       └── JwtTokenService.cs
│   ├── Services
│   │   ├── Interfaces
│   │   ├── AuthService.cs
│   │   ├── ApplicationService.cs
│   │   └── CurrentUserService.cs
│   └── Program.cs
└── WorkflowApp.Api.Tests
    ├── Applications
    ├── Controllers
    ├── Helpers
    └── Serveices
```

### 各層の役割

| 層             | 役割                                                  |
| -------------- | ----------------------------------------------------- |
| Controllers    | HTTP リクエストの受付、入力チェック、レスポンス返却   |
| Services       | 認証処理、申請処理、業務ロジック                      |
| Domain         | エンティティ、Enum 定義                               |
| DTOs           | API のリクエスト・レスポンス定義                      |
| Infrastructure | DB コンテキスト、JWT 発行処理などの外部依存           |
| Tests          | サービス単体テスト、Controller テスト、API 結合テスト |

## ドメイン概要

### User

| 項目         | 説明                              |
| ------------ | --------------------------------- |
| Id           | ユーザーID                        |
| LoginId      | ログインID                        |
| DisplayName  | 表示名                            |
| PasswordHash | ハッシュ化されたパスワード        |
| Role         | ロール。現在は `Applicant` を使用 |
| IsActive     | 有効ユーザーかどうか              |
| CreatedAt    | 作成日時                          |
| UpdatedAt    | 更新日時                          |

### Application

| 項目            | 説明               |
| --------------- | ------------------ |
| Id              | 申請ID             |
| Title           | タイトル           |
| Content         | 申請内容           |
| Status          | 申請ステータス     |
| ApplicantUserId | 申請者のユーザーID |
| CreatedAt       | 作成日時           |
| UpdatedAt       | 更新日時           |

### WorkflowStatus

| 値       | 説明     |
| -------- | -------- |
| Pending  | 申請中   |
| Approved | 承認済み |
| Rejected | 却下     |
| Remanded | 差し戻し |

## API 一覧

### 認証 API

| Method | Endpoint             | 認証 | 説明                       |
| ------ | -------------------- | ---- | -------------------------- |
| POST   | `/api/auth/register` | 不要 | ユーザー登録               |
| POST   | `/api/auth/login`    | 不要 | ログイン、JWT トークン発行 |
| GET    | `/api/auth/me`       | 必要 | 認証済みユーザー情報の取得 |

### 申請 API

| Method | Endpoint                        | 認証 | 説明                             |
| ------ | ------------------------------- | ---- | -------------------------------- |
| POST   | `/api/applications`             | 必要 | 申請作成                         |
| GET    | `/api/applications`             | 必要 | ページネーション付き申請一覧取得 |
| GET    | `/api/applications/list`        | 必要 | 申請一覧取得                     |
| GET    | `/api/applications/{id}`        | 必要 | 申請詳細取得                     |
| PUT    | `/api/applications/{id}`        | 必要 | 申請更新                         |
| PATCH  | `/api/applications/{id}/status` | 必要 | 申請ステータス更新               |
| DELETE | `/api/applications/{id}`        | 必要 | 申請削除                         |

## 認証方法

ログイン API で取得した JWT トークンを、認証が必要な API のリクエストヘッダーに設定します。

```http
Authorization: Bearer {token}
```

JWT にはユーザーID、ログインID、表示名などの情報を含めています。申請 API ではトークン内のユーザーIDを利用し、ログインユーザー本人の申請データだけを操作対象にしています。

## 主なリクエスト例

### ユーザー登録

```http
POST /api/auth/register
Content-Type: application/json

{
  "loginId": "testuser",
  "displayName": "テストユーザー",
  "password": "password123"
}
```

成功時は以下のようなレスポンスを返します。

```json
{
  "message": "ユーザーを登録しました。"
}
```

同じ `loginId` のユーザーが既に存在する場合は `409 Conflict` を返します。

### ログイン

```http
POST /api/auth/login
Content-Type: application/json

{
  "loginId": "testuser",
  "password": "password123"
}
```

成功時は JWT トークンを含むレスポンスを返します。

```json
{
  "token": "{jwt-token}",
  "loginId": "testuser",
  "displayName": "テストユーザー",
  "role": "Applicant",
  "expiresAt": "2026-06-04T12:00:00Z"
}
```

### 認証済みユーザー情報取得

```http
GET /api/auth/me
Authorization: Bearer {token}
```

```json
{
  "userId": 1,
  "loginId": "testuser",
  "displayName": "テストユーザー"
}
```

### 申請作成

```http
POST /api/applications
Content-Type: application/json
Authorization: Bearer {token}

{
  "title": "備品購入申請",
  "content": "開発用キーボードを購入したいです。"
}
```

作成時のステータスは `Pending` です。

### 申請一覧取得

```http
GET /api/applications?page=1&pageSize=10
Authorization: Bearer {token}
```

レスポンス例です。

```json
{
  "items": [
    {
      "id": 1,
      "title": "備品購入申請",
      "status": "Pending",
      "createdAt": "2026-06-04T10:00:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```

### ステータスで絞り込んだ申請一覧取得

```http
GET /api/applications?page=1&pageSize=10&status=Pending
Authorization: Bearer {token}
```

`status` には `Pending`、`Approved`、`Rejected`、`Remanded`、`All` を指定できます。`All` または未指定の場合はステータスで絞り込みません。

### 申請詳細取得

```http
GET /api/applications/1
Authorization: Bearer {token}
```

```json
{
  "id": 1,
  "title": "備品購入申請",
  "content": "開発用キーボードを購入したいです。",
  "status": "Pending",
  "applicantUserId": 1,
  "createdAt": "2026-06-04T10:00:00Z"
}
```

### 申請更新

```http
PUT /api/applications/1
Content-Type: application/json
Authorization: Bearer {token}

{
  "title": "備品購入申請 修正版",
  "content": "開発用キーボードとマウスを購入したいです。"
}
```

成功時は `204 No Content` を返します。

### 申請ステータス更新

```http
PATCH /api/applications/1/status
Content-Type: application/json
Authorization: Bearer {token}

{
  "status": "Approved"
}
```

成功時は `204 No Content` を返します。

### 申請削除

```http
DELETE /api/applications/1
Authorization: Bearer {token}
```

成功時は `204 No Content` を返します。

## クエリパラメータ

### `GET /api/applications`

| パラメータ | 必須 | 初期値 | 説明                                                                                      |
| ---------- | ---- | ------ | ----------------------------------------------------------------------------------------- |
| page       | 任意 | 1      | 取得するページ番号。1未満の場合は1として扱います。                                        |
| pageSize   | 任意 | 10     | 1ページあたりの件数。1未満の場合は10、100を超える場合は100として扱います。                |
| status     | 任意 | なし   | ステータス絞り込み。`All`、`Pending`、`Approved`、`Rejected`、`Remanded` を指定できます。 |

## バリデーション

### ユーザー登録

| 項目        | 条件                       |
| ----------- | -------------------------- |
| loginId     | 必須、50文字以内           |
| displayName | 必須、100文字以内          |
| password    | 必須、8文字以上100文字以内 |

### ログイン

| 項目     | 条件 |
| -------- | ---- |
| loginId  | 必須 |
| password | 必須 |

### 申請作成・更新

| 項目    | 条件               |
| ------- | ------------------ |
| title   | 必須、100文字以内  |
| content | 必須、2000文字以内 |

### 申請ステータス更新

| 項目   | 条件                                                     |
| ------ | -------------------------------------------------------- |
| status | `Pending`、`Approved`、`Rejected`、`Remanded` のいずれか |

## ステータスコード

| ステータス       | 主なケース                                                       |
| ---------------- | ---------------------------------------------------------------- |
| 200 OK           | ログイン、ユーザー登録、一覧取得、詳細取得などが成功した場合     |
| 201 Created      | 申請作成に成功した場合                                           |
| 204 No Content   | 申請更新、ステータス更新、削除に成功した場合                     |
| 400 Bad Request  | 不正なステータス指定、必須項目不足など                           |
| 401 Unauthorized | 未認証、またはJWTからユーザーIDを取得できない場合                |
| 404 Not Found    | 対象の申請が存在しない、またはログインユーザーの申請ではない場合 |
| 409 Conflict     | 同じログインIDのユーザーが既に存在する場合                       |

## セットアップ

### 前提条件

以下がインストールされている必要があります。

- .NET 8 SDK
- Git

### リポジトリの取得

```bash
git clone https://github.com/ssakaguchi/WorkflowApp.Api.git
cd WorkflowApp.Api
```

### 依存パッケージの復元

```bash
dotnet restore
```

### データベースの作成

本プロジェクトでは SQLite を使用しています。

開発環境では、アプリケーション起動時に DB が存在しない場合は `Database.EnsureCreated()` により作成されます。

必要に応じて EF Core のマイグレーションを適用します。

```bash
dotnet ef database update --project WorkflowApp.Api
```

### アプリケーションの起動

```bash
dotnet run --project WorkflowApp.Api
```

起動後、開発環境では Swagger UI から API を確認できます。

```text
https://localhost:{port}/swagger
http://localhost:{port}/swagger
```

## フロントエンド連携

開発環境では CORS 設定により、以下のオリジンからのリクエストを許可しています。

```text
http://localhost:5173
```

React / Vite のフロントエンドから API を呼び出す想定です。

## テスト

テストプロジェクトは `WorkflowApp.Api.Tests` です。

```bash
dotnet test
```

主に以下をテストしています。

- ユーザー登録
- ログイン
- 認証済みユーザー情報取得
- 申請作成
- 申請一覧取得
- ページネーション付き申請一覧取得
- ステータス絞り込み
- 申請詳細取得
- 申請更新
- 申請ステータス更新
- 申請削除
- 認証ユーザーごとのデータ分離
- 入力不正時や対象データ未存在時のレスポンス

[![CI](https://github.com/ssakaguchi/workflowapp-backend/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/ssakaguchi/workflowapp-backend/actions/workflows/ci.yml)
