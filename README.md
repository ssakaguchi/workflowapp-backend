# WorkflowApp.Api

WorkflowApp.Api は、業務ワークフローにおける「申請」機能を想定した ASP.NET Core Web API です。

ユーザー登録・ログインによる JWT 認証を行い、認証済みユーザーごとに申請データの作成・取得・更新・削除を行うことができます。

## 作成背景

デスクトップアプリケーション中心の開発経験から、Web アプリケーション開発へのスキル拡張を目的として作成したポートフォリオです。

特に以下の技術・設計の習得を目的としています。

- ASP.NET Core Web API による REST API 開発
- Entity Framework Core を利用したデータアクセス
- xUnit を利用した単体テスト・API テスト

## 主な機能

- ユーザー登録
- ログイン
- JWT トークン発行
- 認証済みユーザー情報の取得
- 申請の作成
- 申請一覧の取得
- 申請詳細の取得
- 申請の更新
- 申請の削除
- 認証ユーザーごとの申請データ分離
- 入力値バリデーション

## 技術スタック

| 分類           | 使用技術                                                          |
| -------------- | ----------------------------------------------------------------- |
| 言語           | C#                                                                |
| フレームワーク | ASP.NET Core Web API                                              |
| .NET           | .NET 8                                                            |
| ORM            | Entity Framework Core                                             |
| DB             | SQLite                                                            |
| 認証           | JWT Bearer Authentication                                         |
| テスト         | xUnit                                                             |
| テスト補助     | FluentAssertions / NSubstitute / Microsoft.AspNetCore.Mvc.Testing |
| 開発環境       | Visual Studio / Visual Studio Code                                |

## アーキテクチャ

本プロジェクトでは、責務を分離するために以下のようなレイヤード構成を採用しています。

```text
WorkflowApp.Api
├── Controllers
│   ├── AuthController.cs
│   └── ApplicationsController.cs
├── Domain
│   └── Entities
├── DTOs
│   ├── Auth
│   └── Applications
├── Infrastructure
│   ├── Data
│   └── Security
├── Services
│   ├── Interfaces
│   ├── AuthService.cs
│   ├── ApplicationService.cs
│   └── CurrentUserService.cs
└── Program.cs
```

### 各層の役割

| 層             | 役割                                        |
| -------------- | ------------------------------------------- |
| Controllers    | HTTP リクエストの受付、レスポンス返却       |
| Services       | 業務ロジック、認証処理、申請処理            |
| Domain         | エンティティ定義                            |
| DTOs           | API のリクエスト・レスポンス定義            |
| Infrastructure | DB コンテキスト、JWT 発行処理などの外部依存 |
| Tests          | サービス単体テスト、API 結合テスト          |

## API 一覧

### 認証 API

| Method | Endpoint             | 認証 | 説明                       |
| ------ | -------------------- | ---- | -------------------------- |
| POST   | `/api/auth/register` | 不要 | ユーザー登録               |
| POST   | `/api/auth/login`    | 不要 | ログイン、JWT トークン発行 |
| GET    | `/api/auth/me`       | 必要 | 認証済みユーザー情報の取得 |

### 申請 API

| Method | Endpoint                 | 認証 | 説明                           |
| ------ | ------------------------ | ---- | ------------------------------ |
| POST   | `/api/applications`      | 必要 | 申請作成                       |
| GET    | `/api/applications`      | 必要 | ログインユーザーの申請一覧取得 |
| GET    | `/api/applications/{id}` | 必要 | 申請詳細取得                   |
| PUT    | `/api/applications/{id}` | 必要 | 申請更新                       |
| DELETE | `/api/applications/{id}` | 必要 | 申請削除                       |

## 認証方法

ログイン API で取得した JWT トークンを、認証が必要な API のリクエストヘッダーに設定します。

```http
Authorization: Bearer {token}
```

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

### ログイン

```http
POST /api/auth/login
Content-Type: application/json

{
  "loginId": "testuser",
  "password": "password123"
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

## バリデーション

### ユーザー登録

| 項目        | 条件                       |
| ----------- | -------------------------- |
| loginId     | 必須、50文字以内           |
| displayName | 必須、100文字以内          |
| password    | 必須、8文字以上100文字以内 |

### 申請作成・更新

| 項目    | 条件               |
| ------- | ------------------ |
| title   | 必須、100文字以内  |
| content | 必須、2000文字以内 |

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

開発環境では、アプリケーション起動時に DB が存在しない場合は作成されます。

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
```

または

```text
http://localhost:{port}/swagger
```

## フロントエンド連携

開発環境では、以下のフロントエンド URL からのアクセスを許可しています。

```text
http://localhost:5173
```

React + TypeScript 製のフロントエンドアプリケーションから、この API を呼び出す想定です。

## テスト

本プロジェクトでは xUnit を使用してテストを実装しています。

主なテスト対象は以下です。

- 認証サービス
- JWT トークン発行
- 現在のユーザー取得処理
- 申請作成
- 申請一覧取得
- 申請詳細取得
- 申請更新
- 申請削除
- ApplicationsController の API 挙動
- AuthController の認証済みユーザー取得 API

### テスト実行

```bash
dotnet test
```

## 今後の実装候補

今後、以下の機能拡張を検討しています。

- 申請ステータスの承認・却下機能
- 申請種別の追加
- 承認者ロールの追加
- ログ出力の強化

## 補足

このリポジトリはポートフォリオ用途のため、実務で利用される業務ワークフローシステムを簡略化した構成です。

ただし、認証、認可、DB アクセス、サービス層分離、テストなど、Web API 開発で必要となる基本要素を意識して実装しています。
