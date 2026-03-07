using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PRN_Jira.DTOs.Srs;

namespace PRN_Jira.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateSrsPdf(SrsVersionDetailDto document, string projectId)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var snapshot = document.Snapshot;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(content => ComposeContent(content, document, snapshot));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("SRS Document — ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span($"Version {document.VersionNumber}").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" | Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                    x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();
    }

    private static void ComposeHeader(IContainer container)
    {
        container.BorderBottom(1).BorderColor(Colors.Blue.Darken3).PaddingBottom(10).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("SOFTWARE REQUIREMENTS SPECIFICATION")
                    .FontSize(18).Bold().FontColor(Colors.Blue.Darken3);
                col.Item().Text("Jira Project SRS Document")
                    .FontSize(11).FontColor(Colors.Grey.Darken2);
            });
        });
    }

    private static void ComposeContent(IContainer container, SrsVersionDetailDto doc, JiraSnapshotDto snapshot)
    {
        container.Column(col =>
        {
            col.Spacing(12);

            // Document Info
            col.Item().Background(Colors.Blue.Lighten5).Padding(10).Column(info =>
            {
                info.Spacing(4);
                info.Item().Text("Document Information").FontSize(13).Bold().FontColor(Colors.Blue.Darken3);
                InfoRow(info, "Project:", snapshot.ProjectId);
                InfoRow(info, "Version:", $"v{doc.VersionNumber}");
                InfoRow(info, "Description:", doc.Description);
                InfoRow(info, "Created At:", doc.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
                InfoRow(info, "Snapshot Taken:", snapshot.SnapshotTakenAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            });

            // Table of Contents summary
            col.Item().Column(toc =>
            {
                toc.Item().Text("Contents Summary").FontSize(13).Bold().FontColor(Colors.Blue.Darken3);
                toc.Item().Text($"• {snapshot.Releases.Count} Release(s)").FontSize(10);
                toc.Item().Text($"• {snapshot.Epics.Count} Epic(s)").FontSize(10);
                toc.Item().Text($"• {snapshot.UserStories.Count} User Story(-ies)").FontSize(10);
            });

            // Releases Section
            SectionHeader(col, "1. Releases");
            if (snapshot.Releases.Count == 0)
            {
                col.Item().Text("No releases found.").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.RelativeColumn(3);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                    });
                    TableHeader(table, "Name", "Description", "Status", "Release Date");
                    var odd = false;
                    foreach (var r in snapshot.Releases)
                    {
                        odd = !odd;
                        var bg = odd ? Colors.White : Colors.Grey.Lighten4;
                        TableCell(table, r.Name, bg);
                        TableCell(table, r.Description, bg);
                        TableCell(table, r.Status, bg);
                        TableCell(table, r.ReleaseDate ?? "—", bg);
                    }
                });
            }

            // Epics Section
            SectionHeader(col, "2. Epics");
            if (snapshot.Epics.Count == 0)
            {
                col.Item().Text("No epics found.").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(70);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.5f);
                        c.RelativeColumn(1.5f);
                    });
                    TableHeader(table, "Key", "Summary", "Description", "Status", "Assignee");
                    var odd = false;
                    foreach (var e in snapshot.Epics)
                    {
                        odd = !odd;
                        var bg = odd ? Colors.White : Colors.Grey.Lighten4;
                        TableCell(table, e.Key, bg);
                        TableCell(table, e.Summary, bg);
                        TableCell(table, Truncate(e.Description, 120), bg);
                        TableCell(table, e.Status, bg);
                        TableCell(table, e.AssigneeName ?? "Unassigned", bg);
                    }
                });
            }

            // User Stories Section
            SectionHeader(col, "3. User Stories");
            if (snapshot.UserStories.Count == 0)
            {
                col.Item().Text("No user stories found.").Italic().FontColor(Colors.Grey.Medium);
            }
            else
            {
                // Group by Epic
                var grouped = snapshot.UserStories
                    .GroupBy(s => s.EpicKey ?? "No Epic")
                    .OrderBy(g => g.Key);

                foreach (var group in grouped)
                {
                    col.Item().Text($"Epic: {group.Key}").Bold().FontColor(Colors.Blue.Darken2).FontSize(10);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(65);
                            c.RelativeColumn(3);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(1.2f);
                            c.RelativeColumn(1.2f);
                            c.ConstantColumn(50);
                        });
                        TableHeader(table, "Key", "Summary", "Status", "Priority", "Assignee", "Points");
                        var odd = false;
                        foreach (var s in group)
                        {
                            odd = !odd;
                            var bg = odd ? Colors.White : Colors.Grey.Lighten4;
                            TableCell(table, s.Key, bg);
                            TableCell(table, s.Summary, bg);
                            TableCell(table, s.Status, bg);
                            TableCell(table, s.Priority ?? "—", bg);
                            TableCell(table, s.AssigneeName ?? "Unassigned", bg);
                            TableCell(table, s.StoryPoints ?? "—", bg);
                        }
                    });
                    col.Item().Height(4);
                }
            }
        });
    }

    private static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(120).Text(label).Bold().FontSize(10);
            row.RelativeItem().Text(value).FontSize(10);
        });
    }

    private static void SectionHeader(ColumnDescriptor col, string title)
    {
        col.Item().BorderBottom(1).BorderColor(Colors.Blue.Darken3).PaddingBottom(4)
            .Text(title).FontSize(13).Bold().FontColor(Colors.Blue.Darken3);
    }

    private static void TableHeader(TableDescriptor table, params string[] headers)
    {
        table.Header(header =>
        {
            foreach (var h in headers)
            {
                header.Cell().Background(Colors.Blue.Darken3).Padding(5)
                    .Text(h).FontColor(Colors.White).Bold().FontSize(9);
            }
        });
    }

    private static void TableCell(TableDescriptor table, string text, string bg)
    {
        table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
            .Padding(4).Text(text).FontSize(9);
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text[..maxLength] + "…";
    }
}
