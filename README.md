# marifoglu.trade — Toptan Satış

B2B ordering platform connecting **wholesalers** with the **small retailers** they supply. The codebase behind [marifoglu.trade](https://marifoglu.trade).

## Roles

- **Wholesaler** — manages catalog, categories and prices; receives and fulfils orders; issues delivery notes; tracks store credit
- **Store** — browses wholesalers it works with, places orders, follows deliveries and balance
- **Admin** — manages users and wholesalers

## Components

| Folder | What it is | Stack |
|---|---|---|
| `backend/` | REST API with custom exception handling middleware and DTO layer | ASP.NET Core (.NET 10) · EF Core · PostgreSQL |
| `frontend/` | Web app for all three roles | React · TypeScript · Vite |
| `mobile/marifoglu_store/` | Mobile app for stores | Flutter |
| `backup/` | Scheduled PostgreSQL backup container | Bash · Docker |

## Highlights

- JWT auth, role-based authorization
- Credit (veresiye) tracking between wholesaler and store
- Delivery notes and order status flow
- Notifications (incl. Telegram bot)
- Automated database backups — see [DOCS/RESTORE.md](DOCS/RESTORE.md)

## Running

```bash
cp .env.example .env        # DB password, Jwt__Secret, ...
docker compose up -d --build
```
