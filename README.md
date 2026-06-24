# Workflow App Backend

[![CI](https://github.com/ssakaguchi/workflowapp-backend/actions/workflows/ci.yml/badge.svg?branch=master)](https://github.com/ssakaguchi/workflowapp-backend/actions/workflows/ci.yml)

Workflow App Backend は、業務ワークフローにおける「申請・承認」機能を想定した ASP.NET Core Web API です。

ユーザー登録・ログインによる JWT 認証を行い、認証済みユーザーごとに申請データの作成・取得・更新・削除を行えます。  
また、申請作成時に承認者を指定し、承認者が自分に回付された申請を承認・却下できるようにしています。

---

## 作成背景

デスクトップアプリケーション中心の開発経験から、Web アプリケーション開発へのスキル拡張を目的として作成したポートフォリオです。

特に以下の技術・設計の習得を目的としています。

- ASP.NET Core Web API による REST API 開発
- JWT を利用した認証・認可
- Entity Framework Core を利用したデータアクセス
- 申請者・承認者を考慮した業務ロジックの実装
- xUnit を利用した単体テスト・API 結合テスト
- GitHub Actions による CI 実行

---

## 主な機能

### 認証機能

- ユーザー登録
- ログイン
- JWT トークン発行
- 認証済みユーザー情報の取得
- パスワードハッシュ化
- ログイン ID の一意制約
- ロール情報の保持
  - `Applicant`
  - `Approver`

### ユーザー機能

- 承認者一覧の取得
- 申請作成時に指定可能な承認者の取得

### 申請機能

- 申請の作成
- 申請一覧の取得
- 申請一覧のページネーション
- 申請一覧のステータス絞り込み
- 申請詳細の取得
- 申請の更新
- 申請の削除
- 認証ユーザーごとの申請データ分離
- 入力値バリデーション

### 承認ワークフロー機能

- 申請作成時の承認者指定
- 申請に紐づく承認ステップの作成
- 承認者による申請の承認
- 承認者による申請の却下
- 承認ステップのステータス更新
- 承認者本人かどうかのチェック
- 申請者本人、または承認者のみ申請詳細を参照可能
- 承認者のみステータス更新可能

---

## 技術スタック

| 分類           | 使用技術                                                                             |
| -------------- | ------------------------------------------------------------------------------------ |
| 言語           | C#                                                                                   |
| フレームワーク | ASP.NET Core Web API                                                                 |
| .NET           | .NET 8                                                                               |
| ORM            | Entity Framework Core                                                                |
| DB             | SQLite                                                                               |
| 認証           | JWT Bearer Authentication                                                            |
| API 確認       | Swagger / Swagger UI                                                                 |
| テスト         | xUnit                                                                                |
| テスト補助     | FluentAssertions / NSubstitute / Microsoft.AspNetCore.Mvc.Testing / EF Core InMemory |
| CI             | GitHub Actions                                                                       |
| 開発環境       | Visual Studio / Visual Studio Code                                                   |

---

## アーキテクチャ

責務を分離するため、Controller / Service / DTO / Domain / Infrastructure に分けた構成にしています。

```text
WorkflowApp.Api
├── WorkflowApp.Api
│   ├── Controllers
│   │   ├── ApplicationsController.cs
│   │   ├── AuthController.cs
│   │   └── UsersController.cs
│   ├── Domain
│   │   ├── Entities
│   │   │   ├── Application.cs
│   │   │   ├── ApprovalStep.cs
│   │   │   └── User.cs
│   │   └── Enums
│   │       ├── ApprovalStepStatus.cs
│   │       ├── UserRole.cs
│   │       └── WorkflowStatus.cs
│   ├── DTOs
│   │   ├── Applications
│   │   ├── Auth
│   │   └── Users
│   ├── Infrastructure
│   │   ├── Data
│   │   │   └── AppDbContext.cs
│   │   ├── Seed
│   │   └── Security
│   │       └── JwtTokenService.cs
│   ├── Services
│   │   ├── Interfaces
│   │   ├── ApplicationService.cs
│   │   ├── AuthService.cs
│   │   ├── CurrentUserService.cs
│   │   └── UserService.cs
│   └── Program.cs
└── WorkflowApp.Api.Tests
    ├── Applications
    ├── Controllers
    ├── Helpers
    └── Services
```

### 各層の役割

| 層             | 役割                                                  |
| -------------- | ----------------------------------------------------- |
| Controllers    | HTTP リクエストの受付、入力チェック、レスポンス返却   |
| Services       | 認証処理、申請処理、承認処理、業務ロジック            |
| Domain         | エンティティ、Enum 定義                               |
| DTOs           | API のリクエスト・レスポンス定義                      |
| Infrastructure | DB コンテキスト、JWT 発行、Seed 処理などの外部依存    |
| Tests          | サービス単体テスト、Controller テスト、API 結合テスト |

---

## ドメイン概要

### User

| 項目         | 説明                       |
| ------------ | -------------------------- |
| Id           | ユーザー ID                |
| LoginId      | ログイン ID                |
| DisplayName  | 表示名                     |
| PasswordHash | ハッシュ化されたパスワード |
| Role         | ユーザーロール             |
| IsActive     | 有効ユーザーかどうか       |
| CreatedAt    | 作成日時                   |
| UpdatedAt    | 更新日時                   |

### Application

| 項目            | 説明                |
| --------------- | ------------------- |
| Id              | 申請 ID             |
| Title           | タイトル            |
| Content         | 申請内容            |
| Status          | 申請ステータス      |
| ApplicantUserId | 申請者のユーザー ID |
| ApplicantUser   | 申請者ユーザー      |
| ApprovalSteps   | 承認ステップ一覧    |
| CreatedAt       | 作成日時            |
| UpdatedAt       | 更新日時            |

### ApprovalStep

| 項目           | 説明                     |
| -------------- | ------------------------ |
| Id             | 承認ステップ ID          |
| ApplicationId  | 申請 ID                  |
| Application    | 申請                     |
| StepOrder      | 承認順                   |
| ApproverUserId | 承認者のユーザー ID      |
| ApproverUser   | 承認者ユーザー           |
| Status         | 承認ステップのステータス |
| CreatedAt      | 作成日時                 |
| UpdatedAt      | 更新日時                 |

### UserRole

| 値        | 説明   |
| --------- | ------ |
| Applicant | 申請者 |
| Approver  | 承認者 |

### WorkflowStatus

| 値       | 説明     |
| -------- | -------- |
| Pending  | 申請中   |
| Approved | 承認済み |
| Rejected | 却下     |
| Remanded | 差し戻し |

### ApprovalStepStatus

| 値       | 説明     |
| -------- | -------- |
| Pending  | 承認待ち |
| Approved | 承認済み |
| Rejected | 却下     |

---

## API 一覧

### 認証 API

| Method | Endpoint             | 認証 | 説明                       |
| ------ | -------------------- | ---- | -------------------------- |
| POST   | `/api/auth/register` | 不要 | ユーザー登録               |
| POST   | `/api/auth/login`    | 不要 | ログイン、JWT トークン発行 |
| GET    | `/api/auth/me`       | 必要 | 認証済みユーザー情報の取得 |

### ユーザー API

| Method | Endpoint               | 認証 | 説明             |
| ------ | ---------------------- | ---- | ---------------- |
| GET    | `/api/users/approvers` | 必要 | 承認者一覧の取得 |

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

---

## 認証・認可

ログイン API で取得した JWT トークンを、認証が必要な API のリクエストヘッダーに設定します。

```http
Authorization: Bearer {token}
```

JWT には、ユーザー ID、ログイン ID、表示名、ロールなどの情報を含めています。

申請 API では、JWT から取得したユーザー ID とロールを利用して、以下のようにアクセス制御しています。

| 操作               | 許可条件                           |
| ------------------ | ---------------------------------- |
| 申請作成           | 認証済みユーザー                   |
| 自分の申請一覧取得 | 申請者本人                         |
| 申請詳細取得       | 申請者本人、または承認者本人       |
| 申請更新           | 申請者本人                         |
| 申請削除           | 申請者本人                         |
| 承認・却下         | 承認者ロール、かつ対象申請の承認者 |

---

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
  "displayName": "テストユーザー",
  "role": "Applicant"
}
```

### 承認者一覧取得

```http
GET /api/users/approvers
Authorization: Bearer {token}
```

```json
[
  {
    "id": 2,
    "displayName": "テスト承認者"
  }
]
```

### 申請作成

```http
POST /api/applications
Content-Type: application/json
Authorization: Bearer {token}

{
  "title": "備品購入申請",
  "content": "開発用キーボードを購入したいです。",
  "approverUserId": 2
}
```

作成時の申請ステータスは `Pending` です。  
あわせて、指定した承認者に紐づく承認ステップを作成します。

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

`status` には `Pending`、`Approved`、`Rejected`、`Remanded`、`All` を指定できます。  
`All` または未指定の場合はステータスで絞り込みません。

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
  "createdAt": "2026-06-04T10:00:00Z",
  "approvalSteps": [
    {
      "id": 1,
      "stepOrder": 1,
      "approverUserId": 2,
      "approverDisplayName": "テスト承認者",
      "status": "Pending"
    }
  ]
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

承認・却下は、対象申請の承認者として登録されているユーザーのみ実行できます。

### 申請削除

```http
DELETE /api/applications/1
Authorization: Bearer {token}
```

成功時は `204 No Content` を返します。

---

## クエリパラメータ

### `GET /api/applications`

| パラメータ | 必須 | 初期値 | 説明                                                                                      |
| ---------- | ---- | ------ | ----------------------------------------------------------------------------------------- |
| page       | 任意 | 1      | 取得するページ番号。1 未満の場合は 1 として扱います。                                     |
| pageSize   | 任意 | 10     | 1 ページあたりの件数。1 未満の場合は 10、100 を超える場合は 100 として扱います。          |
| status     | 任意 | なし   | ステータス絞り込み。`All`、`Pending`、`Approved`、`Rejected`、`Remanded` を指定できます。 |

---

## バリデーション

### ユーザー登録

| 項目        | 条件                          |
| ----------- | ----------------------------- |
| loginId     | 必須、50 文字以内             |
| displayName | 必須、100 文字以内            |
| password    | 必須、8 文字以上 100 文字以内 |

### ログイン

| 項目     | 条件 |
| -------- | ---- |
| loginId  | 必須 |
| password | 必須 |

### 申請作成

| 項目           | 条件                |
| -------------- | ------------------- |
| title          | 必須、100 文字以内  |
| content        | 必須、2000 文字以内 |
| approverUserId | 必須                |

### 申請更新

| 項目    | 条件                |
| ------- | ------------------- |
| title   | 必須、100 文字以内  |
| content | 必須、2000 文字以内 |

### 申請ステータス更新

| 項目   | 条件                                                    |
| ------ | ------------------------------------------------------- |
| status | `Approved`、`Rejected` など、更新可能なステータスを指定 |

---

## ステータスコード

| ステータス       | 主なケース                                                     |
| ---------------- | -------------------------------------------------------------- |
| 200 OK           | ログイン、ユーザー登録、一覧取得、詳細取得などが成功した場合   |
| 201 Created      | 申請作成に成功した場合                                         |
| 204 No Content   | 申請更新、ステータス更新、削除に成功した場合                   |
| 400 Bad Request  | 不正なステータス指定、必須項目不足など                         |
| 401 Unauthorized | 未認証、または JWT からユーザー ID を取得できない場合          |
| 403 Forbidden    | 権限がないユーザーが承認・却下などを実行しようとした場合       |
| 404 Not Found    | 対象の申請が存在しない、または参照権限のない申請を取得した場合 |
| 409 Conflict     | 同じログイン ID のユーザーが既に存在する場合                   |

---

## セットアップ

### 前提条件

以下がインストールされている必要があります。

- .NET 8 SDK
- Git

### リポジトリの取得

```bash
git clone https://github.com/ssakaguchi/workflowapp-backend.git
cd workflowapp-backend
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

---

## フロントエンド連携

開発環境では CORS 設定により、以下のオリジンからのリクエストを許可しています。

```text
http://localhost:5173
```

React / Vite のフロントエンドから API を呼び出す想定です。

---

## テスト

テストプロジェクトは `WorkflowApp.Api.Tests` です。

```bash
dotnet test
```

主に以下をテストしています。

- ユーザー登録
- ログイン
- 認証済みユーザー情報取得
- 承認者一覧取得
- 申請作成
- 申請一覧取得
- ページネーション付き申請一覧取得
- ステータス絞り込み
- 申請詳細取得
- 申請更新
- 申請ステータス更新
- 申請削除
- 認証ユーザーごとのデータ分離
- 承認者本人のみ承認・却下できること
- 権限がないユーザーの操作が拒否されること
- 入力不正時や対象データ未存在時のレスポンス

---

## CI

GitHub Actions で、Pull Request および `master` ブランチへの push 時にテストを実行します。

主な実行内容は以下です。

- .NET SDK のセットアップ
- 依存関係の復元
- ビルド
- テスト実行

---

## 補足

本リポジトリは、学習およびポートフォリオ用途で作成しています。  
React / TypeScript のフロントエンド実装とあわせて、段階的に機能を拡張しています。
