# 🔍 Factlens - Back-end API
> An AI-powered fact-checking and news verification platform built with .NET.

[![.NET](https://img.shields.io/badge/.NET-8.0-512bd4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Status: Development](https://img.shields.io/badge/Status-In--Development-orange)]()

## 📌 Project Overview
**Factlens** is a specialized platform designed to verify news and information using Artificial Intelligence. This repository contains the core API responsible for handling data processing, user authentication, and integration with AI models via RAG (Retrieval-Augmented Generation).

## 🛠 Tech Stack
*   **Backend Framework:** ASP.NET Core Web API (8.0)
*   **ORM:** Entity Framework Core
*   **Database:** SQL Server
*   **Security:** JWT Authentication & ASP.NET Core Identity
*   **AI Integration:** Python (FastAPI) & LLMs with RAG
*   **Documentation:** Swagger / OpenAPI

## 🚀 Getting Started

### 1. Prerequisites
*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   SQL Server (LocalDB or Express)
*   Visual Studio 2022 or VS Code

### 2. Configuration
To keep sensitive information secure, the `appsettings.json` file is excluded from the repository.
1.  Locate `appsettings.Example.json` in the root folder.
2.  Create a copy of it and rename it to `appsettings.json`.
3.  Update the values with your actual database connection strings, JWT keys, and API credentials.

> **Tip:** For local development, it is highly recommended to use **User Secrets** to store sensitive data.

### 3. Database Setup
Apply migrations to create the database schema:
```bash
dotnet ef database update
