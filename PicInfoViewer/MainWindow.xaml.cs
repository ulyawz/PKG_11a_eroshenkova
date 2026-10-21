using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace PicInfoViewer;
using WpfAnimatedGif;
using static PicInfoViewer.ImageRow;

public partial class MainWindow : Window
{
    static readonly string[] Extensions = { ".jpg", ".jpeg", ".gif", ".tif", ".tiff", ".bmp", ".png", ".pcx" };

    CancellationTokenSource? _cancelSource;
    ICollectionView? _filteredView;

    public MainWindow() { InitializeComponent(); }


    void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true) _ = ScanFolder(dlg.FolderName);
    }

    void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] p && p.Length > 0 && Directory.Exists(p[0]))
            _ = ScanFolder(p[0]);
    }

    void Cancel_Click(object sender, RoutedEventArgs e) => _cancelSource?.Cancel();

    async Task ScanFolder(string folder)
    {
        if (_cancelSource != null) return;
        _cancelSource = new CancellationTokenSource();
        var ct = _cancelSource.Token;

        BtnOpen.IsEnabled = false;
        BtnCancel.IsEnabled = true;
        ImagesGrid.ItemsSource = null;
        ShowPreview(null);
        StatusText.Text = "Поиск файлов…";
        ScanBar.IsIndeterminate = true;
        var sw = Stopwatch.StartNew();
        bool sub = ChkRecursive.IsChecked == true;

        try
        {
            var opt = new EnumerationOptions { RecurseSubdirectories = sub, IgnoreInaccessible = true };
            var files = await Task.Run(() => Directory.GetFiles(folder, "*", opt)
                .Where(f => Extensions.Contains(Path.GetExtension(f).ToLower())).ToArray());

            ScanBar.IsIndeterminate = false;
            ScanBar.Maximum = Math.Max(1, files.Length);
            ScanBar.Value = 0;
            var rows = new ImageRow?[files.Length];
            int done = 0;

            var work = Task.Run(() => Parallel.For(0, files.Length, new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 8 }, i =>
            {
                rows[i] = ImageReader.Read(files[i]);
                Interlocked.Increment(ref done);
            }));

            while (!work.IsCompleted)
            {
                await Task.Delay(100);
                ScanBar.Value = done;
                StatusText.Text = $"Обработано {done} из {files.Length}";
            }
            try { await work; } catch (OperationCanceledException) { }

            var result = rows.Where(r => r != null).Cast<ImageRow>().ToArray();
            _filteredView = CollectionViewSource.GetDefaultView(result);
            _filteredView.Filter = NameMatches;
            ImagesGrid.ItemsSource = _filteredView;

            ScanBar.Value = done;
            StatusText.Text = (ct.IsCancellationRequested ? "Остановлено: " : "Готово: ") +
                          $"{result.Length} файлов за {sw.Elapsed.TotalSeconds:0.0} с";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Ошибка: " + ex.Message;
        }
        finally
        {
            _cancelSource = null;
            BtnOpen.IsEnabled = true;
            BtnCancel.IsEnabled = false;
            ScanBar.IsIndeterminate = false;
        }
    }


    bool NameMatches(object o) =>
        o is ImageRow r && (string.IsNullOrWhiteSpace(SearchBox.Text) ||
                       r.Name.Contains(SearchBox.Text.Trim(), StringComparison.OrdinalIgnoreCase));

    void Search_Changed(object sender, TextChangedEventArgs e) => _filteredView?.Refresh();


    void ImagesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowPreview(ImagesGrid.SelectedItem as ImageRow);

    void ShowPreview(ImageRow? row)
    {
        ImageBehavior.SetAnimatedSource(PreviewImage, null);
        PreviewImage.Source = null;
        PreviewHint.Visibility = Visibility.Visible;
        PreviewCaption.Text = row == null ? "" : $"{row.Name}\n{row.Dimensions} · {row.Format}";
        if (row == null) { PreviewHint.Text = "Выберите файл в таблице"; return; }

        try
        {
            var bi = new BitmapImage();
            if (row.Format == "GIF")
            {
                bi.BeginInit();
                bi.UriSource = new Uri(row.FullPath);
                bi.EndInit();
                ImageBehavior.SetAnimatedSource(PreviewImage, bi);
            }
            else
            {
                using var fs = File.OpenRead(row.FullPath);
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.OnLoad;
                bi.DecodePixelWidth = 400;
                bi.StreamSource = fs;
                bi.EndInit();
                bi.Freeze();
                PreviewImage.Source = bi;
            }
            PreviewHint.Visibility = Visibility.Collapsed;
        }
        catch
        {
            PreviewHint.Text = "Предпросмотр недоступен для этого файла (например, PCX WPF не показывает)";
        }
    }
} 