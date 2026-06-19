# Platform.Payment.API

## Kafka config

- Local development defaults live in `appsettings.Development.json`.
- Base schema stays in `appsettings.json` with empty values only.
- Deploy/runtime values come from `Platform.IaC/docker-compose.yml` and environment variables.
- Kafka consumer offset reset is configured through `Kafka:ConsumerAutoOffsetReset` or `Kafka__ConsumerAutoOffsetReset`.

## Public

- [ ] `POST /api/payments/webhooks/{provider}` - webhook provider, khong thay UI goi truc tiep
- [ ] `GET /api/payments/sandbox/checkout/{referenceCode}` - sandbox payment page
- [ ] `GET /api/payments/sandbox/complete/{referenceCode}` - sandbox callback completion

## Authenticated

- [x] `GET /api/payments/me/transactions` - `Platform.PaymentUI/src/features/dashboard/api/transactionsApi.ts`
