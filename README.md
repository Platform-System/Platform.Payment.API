# Platform.Payment.API

## Quy uoc

- `[x]` = da thay frontend dang goi
- `[ ]` = chua thay frontend nao trong repo hien tai goi toi

## Public

- [ ] `POST /api/payments/webhooks/{provider}` - webhook provider, khong thay UI goi truc tiep
- [ ] `GET /api/payments/sandbox/checkout/{referenceCode}` - sandbox payment page
- [ ] `GET /api/payments/sandbox/complete/{referenceCode}` - sandbox callback completion

## Notes

- Chua thay `AdminUI`, `MerchantUI`, `PortalUI` goi truc tiep trong repo hien tai.
- Co kha nang endpoint nay duoc goi gian tiep qua redirect/webhook flow thay vi frontend app call thang.
