# 📦 Student Bazaar (Bazaar)

**Bazaar** is a university marketplace platform that enables students to **buy, sell, and rent products** easily inside campus.  
The system aims to create a **secure, fast, and user-friendly environment** for student transactions.

---

## 🚀 Features

- 🛍 Buy & Sell products inside campus
- 🔄 Product rental system (daily pricing – *new feature added*)
- 🔐 User authentication & Identity integration
- 🔔 Notification system for user interactions
- 🏷 Categories & filtering for easier browsing
- 🖼 Image upload support for products
- 🎨 Modern UI with improved styling
- 🗄 EF Core Migrations included
- 🛡 Secure Login UI (`_LoginPartial.cshtml` integrated)

---

## 🛠 Tech Stack

| Category | Technologies |
|--------|------------------------------|
| Backend | **ASP.NET Core MVC** |
| Database | **SQL Server + Entity Framework Core** |
| Frontend | **Razor Views + Bootstrap + CSS** |
| ORM | **EF Core** |
| Tools | **Visual Studio / VS Code + GitHub** |

---

## 📂 Project Structure

```bash
StudentBazaar/
├── StudentBazaar/                   # MVC Web Project
│   ├── Controllers/
│   ├── Views/                       # Razor Views
│   ├── wwwroot/                     # Assets (CSS, JS, images)
│   └── ...
│
├── StudentBazaar.DataAccess/        # EF Core + Migrations
│   ├── Migrations/
│   └── ...
└── ...
