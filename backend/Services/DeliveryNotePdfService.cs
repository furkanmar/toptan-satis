using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WholesaleApi.Data;
using WholesaleApi.Entities;

namespace WholesaleApi.Services;

public class DeliveryNotePdfService(AppDbContext db)
{
    public async Task<byte[]> GenerateAsync(Guid noteId, Guid wholesalerId)
    {
        var note = await db.DeliveryNotes
            .Include(dn => dn.Items)
            .Include(dn => dn.Wholesaler)
            .Include(dn => dn.Store)
            .FirstOrDefaultAsync(dn => dn.Id == noteId && dn.WholesalerId == wholesalerId)
            ?? throw new KeyNotFoundException("Sevk irsaliyesi bulunamadı");

        return GeneratePdf(note);
    }

    public async Task<byte[]> GenerateForStoreAsync(Guid noteId, Guid storeId)
    {
        var note = await db.DeliveryNotes
            .Include(dn => dn.Items)
            .Include(dn => dn.Wholesaler)
            .Include(dn => dn.Store)
            .FirstOrDefaultAsync(dn => dn.Id == noteId && dn.StoreId == storeId)
            ?? throw new KeyNotFoundException("Sevk irsaliyesi bulunamadı");

        return GeneratePdf(note);
    }

    private static byte[] GeneratePdf(DeliveryNote note)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, note));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Sayfa ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();

        void ComposeHeader(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("SEVK İRSALİYESİ").Bold().FontSize(16);
                        c.Item().Text($"İrsaliye No: {note.NoteNumber}").FontSize(11);
                        c.Item().Text($"Tarih: {note.IssueDate:dd/MM/yyyy}");
                        if (!string.IsNullOrEmpty(note.Ettn))
                            c.Item().Text($"ETTN: {note.Ettn}").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    row.ConstantItem(150).Column(c =>
                    {
                        c.Item().AlignRight().Text(note.Status switch
                        {
                            DeliveryNoteStatus.Issued => "DÜZENLENDI",
                            DeliveryNoteStatus.Cancelled => "İPTAL EDİLDİ",
                            _ => "TASLAK"
                        }).Bold().FontColor(note.Status == DeliveryNoteStatus.Cancelled
                            ? Colors.Red.Medium
                            : Colors.Green.Medium);
                    });
                });

                col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Medium);
            });
        }

        void ComposeContent(IContainer container, DeliveryNote note)
        {
            container.Column(col =>
            {
                // Gönderen / Alıcı
                col.Item().PaddingTop(12).Row(row =>
                {
                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("GÖNDEREN").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().PaddingTop(4).Text(note.Wholesaler.CompanyName).Bold();
                        if (!string.IsNullOrEmpty(note.Wholesaler.TaxNumber))
                            c.Item().Text($"Vergi No: {note.Wholesaler.TaxNumber}").FontSize(9);
                        if (!string.IsNullOrEmpty(note.SourceAddress))
                            c.Item().Text(note.SourceAddress).FontSize(9);
                        if (!string.IsNullOrEmpty(note.Wholesaler.Phone))
                            c.Item().Text($"Tel: {note.Wholesaler.Phone}").FontSize(9);
                    });

                    row.ConstantItem(16);

                    row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("ALICI").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                        c.Item().PaddingTop(4).Text(note.Store.StoreName).Bold();
                        if (!string.IsNullOrEmpty(note.DestinationAddress))
                            c.Item().Text(note.DestinationAddress).FontSize(9);
                        if (!string.IsNullOrEmpty(note.Store.Phone))
                            c.Item().Text($"Tel: {note.Store.Phone}").FontSize(9);
                    });
                });

                // Taşıma bilgileri (varsa)
                var hasTransport = !string.IsNullOrEmpty(note.VehiclePlate) || !string.IsNullOrEmpty(note.DriverName);
                if (hasTransport)
                {
                    col.Item().PaddingTop(8).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Row(row =>
                    {
                        row.AutoItem().Text("Taşıma Bilgisi:").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                        if (!string.IsNullOrEmpty(note.VehiclePlate))
                        {
                            row.AutoItem().PaddingLeft(12).Text($"Araç: {note.VehiclePlate}");
                        }
                        if (!string.IsNullOrEmpty(note.DriverName))
                        {
                            row.AutoItem().PaddingLeft(12).Text($"Sürücü: {note.DriverName}");
                        }
                    });
                }

                // Ürün tablosu
                col.Item().PaddingTop(16).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(30);    // #
                        cols.RelativeColumn(4);      // Ürün
                        cols.RelativeColumn(1.5f);   // Birim
                        cols.RelativeColumn(1);      // Sipariş
                        cols.RelativeColumn(1);      // Sevk
                        cols.RelativeColumn(1.5f);   // Birim Fiyat
                        cols.RelativeColumn(1);      // KDV
                        cols.RelativeColumn(1.5f);   // Tutar
                    });

                    // Header
                    table.Header(header =>
                    {
                        void HeaderCell(string text) =>
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(5)
                                .Text(text).Bold().FontSize(9);

                        HeaderCell("#");
                        HeaderCell("Ürün Adı");
                        HeaderCell("Birim");
                        HeaderCell("Sipariş Qty");
                        HeaderCell("Sevk Qty");
                        HeaderCell("Birim Fiyat");
                        HeaderCell("KDV %");
                        HeaderCell("Tutar");
                    });

                    // Rows
                    var totalNet = 0m;
                    var totalVat = 0m;
                    var idx = 1;

                    foreach (var item in note.Items)
                    {
                        var lineTotal = item.UnitPrice * item.QuantityShipped;
                        var lineVat = lineTotal * (item.VatRate / 100m);
                        totalNet += lineTotal;
                        totalVat += lineVat;

                        var bgColor = idx % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                        void Cell(Action<TextDescriptor> textAction) =>
                            table.Cell().Background(bgColor).Padding(4).Text(textAction);

                        Cell(t => t.Span(idx.ToString()).FontSize(9));
                        Cell(t =>
                        {
                            t.Span(item.ProductName).FontSize(9);
                            if (!string.IsNullOrEmpty(item.ProductBrand))
                                t.Span($"\n{item.ProductBrand}").FontSize(8).FontColor(Colors.Grey.Darken2);
                        });
                        Cell(t => t.Span($"{item.UnitType} ({item.ContentQty})").FontSize(9));
                        Cell(t => t.Span(item.QuantityOrdered.ToString()).FontSize(9));
                        Cell(t => t.Span(item.QuantityShipped.ToString()).Bold().FontSize(9));
                        Cell(t => t.Span($"₺{item.UnitPrice:N2}").FontSize(9));
                        Cell(t => t.Span($"%{item.VatRate}").FontSize(9));
                        Cell(t => t.Span($"₺{lineTotal:N2}").FontSize(9));

                        idx++;
                    }

                    // Totals
                    table.Cell().ColumnSpan(6).Padding(4).Text("").FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Ara Toplam").Bold().FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text($"₺{totalNet:N2}").Bold().FontSize(9);

                    table.Cell().ColumnSpan(6).Padding(4).Text("").FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("KDV Toplam").Bold().FontSize(9);
                    table.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text($"₺{totalVat:N2}").Bold().FontSize(9);

                    table.Cell().ColumnSpan(6).Padding(4).Text("").FontSize(9);
                    table.Cell().Background(Colors.Blue.Lighten4).Padding(4).Text("GENEL TOPLAM").Bold().FontSize(10);
                    table.Cell().Background(Colors.Blue.Lighten4).Padding(4)
                        .Text($"₺{(totalNet + totalVat):N2}").Bold().FontSize(10);
                });

                // Notlar
                if (!string.IsNullOrEmpty(note.Notes))
                {
                    col.Item().PaddingTop(12).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
                    {
                        c.Item().Text("Notlar:").Bold().FontSize(9);
                        c.Item().PaddingTop(4).Text(note.Notes).FontSize(9);
                    });
                }

                // İmza alanları
                col.Item().PaddingTop(32).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        c.Item().PaddingTop(4).AlignCenter().Text("Gönderen İmza / Kaşe").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });

                    row.ConstantItem(40);

                    row.RelativeItem().Column(c =>
                    {
                        c.Item().LineHorizontal(1).LineColor(Colors.Grey.Medium);
                        c.Item().PaddingTop(4).AlignCenter().Text("Teslim Alan İmza").FontSize(9).FontColor(Colors.Grey.Darken2);
                    });
                });
            });
        }
    }
}
