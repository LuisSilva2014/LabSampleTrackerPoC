using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Media;
using LabSampleTracker.Desktop.Models;
using LabSampleTracker.Desktop.Services;

namespace LabSampleTracker.Desktop;

public partial class MainWindow : Window
{
    private readonly SampleApiClient _apiClient = new(ApiSettings.BaseUrl);
    private readonly ObservableCollection<SampleRow> _samples = new();

    private bool _isEditing;
    private bool _isBusy;
    private bool _refreshReady;

    public MainWindow()
    {
        InitializeComponent();
        SamplesGrid.ItemsSource = _samples;
        StatusCombo.ItemsSource = new[] { "Pending", "Processing" };
        StatusCombo.SelectedIndex = 0;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadSamplesAsync();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _apiClient.Dispose();
    }

    private async Task LoadSamplesAsync()
    {
        try
        {
            SetBusy(true);
            ShowStatus("Loading samples...");

            var samples = await _apiClient.GetAllAsync();
            _samples.Clear();

            foreach (var sample in samples)
            {
                AddRow(sample);
            }

            UpdateCount();
            ShowStatus($"Loaded {_samples.Count} samples.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ShowStatus("Cannot reach the API. Start LabSampleTracker.WebApi, then open this window again.", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        _isEditing = false;
        EditorPanel.Header = "Add sample";
        IdBox.IsEnabled = true;
        IdBox.Text = SuggestNextId().ToString();
        NameBox.Text = "";
        StatusCombo.SelectedItem = "Pending";
        EditorHint.Text = "New samples are stored by the API and added to the end of the list.";
        EditorPanel.Visibility = Visibility.Visible;
        NameBox.Focus();
    }

    private void ModifyButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = LastCheckedRow();
        if (selected is null)
        {
            return;
        }

        _isEditing = true;
        EditorPanel.Header = "Edit sample";
        IdBox.IsEnabled = false;
        IdBox.Text = selected.Id.ToString();
        NameBox.Text = selected.Name;
        StatusCombo.SelectedItem = selected.Status;
        EditorHint.Text = "Id is locked. Save updates this sample instead of creating a new one.";
        EditorPanel.Visibility = Visibility.Visible;
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        const int count = 1_000_000;

        try
        {
            _refreshReady = false;
            SetBusy(true);
            ShowStatus("Creating 1,000,000 records in the background. Refresh stays off until this finishes...");

            // await returns to the UI thread while the API builds the rows.
            var result = await _apiClient.GenerateAsync(count);

            ShowStatus("Records are in memory. Waiting 10 seconds before Refresh can load the grid...");
            await Task.Delay(TimeSpan.FromSeconds(10));

            _refreshReady = true;
            ShowStatus($"Ready. API memory holds {result.Total:N0} records. Refresh loads them into the grid.");
        }
        catch (ApiException ex)
        {
            ShowStatus(ex.Message, isError: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ShowStatus("Cannot reach the API. Start LabSampleTracker.WebApi and try again.", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadSamplesAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = _samples.Where(sample => sample.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var idList = string.Join(", ", selected.Select(sample => sample.Id));
        var answer = MessageBox.Show(
            $"Would you like to remove the selected records?\n\n{idList}",
            "Delete samples",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            SetBusy(true);
            var ids = selected.Select(sample => sample.Id).ToList();
            await _apiClient.DeleteAsync(ids);

            foreach (var row in selected)
            {
                _samples.Remove(row);
            }

            if (EditorPanel.Visibility == Visibility.Visible &&
                int.TryParse(IdBox.Text, out var openId) &&
                ids.Contains(openId))
            {
                HideEditor();
            }

            UpdateCount();
            ShowStatus($"Removed {ids.Count} sample(s).");
        }
        catch (ApiException ex)
        {
            ShowStatus(ex.Message, isError: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ShowStatus("Cannot reach the API. Start LabSampleTracker.WebApi and try again.", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadForm(out var id, out var name, out var status))
        {
            return;
        }

        if (!_isEditing && _samples.Any(sample => sample.Id == id))
        {
            ShowStatus($"A sample with id {id} already exists.", isError: true);
            return;
        }

        var draft = new SampleDto
        {
            Id = id,
            Name = name,
            Status = status
        };

        try
        {
            SetBusy(true);

            // Modify freezes the id and sets _isEditing, so Save updates.
            // Add leaves the id editable, so Save creates a row at the end.
            if (_isEditing)
            {
                var updated = await _apiClient.UpdateAsync(draft);
                var row = _samples.First(sample => sample.Id == updated.Id);
                row.Name = updated.Name;
                row.Status = updated.Status;
                ShowStatus($"Updated sample {updated.Id}.");
            }
            else
            {
                var created = await _apiClient.AddAsync(draft);
                AddRow(created);
                ShowStatus($"Saved sample {created.Id}.");
            }

            HideEditor();
            UpdateCount();
        }
        catch (ApiException ex)
        {
            ShowStatus(ex.Message, isError: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ShowStatus("Cannot reach the API. Start LabSampleTracker.WebApi and try again.", isError: true);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        HideEditor();
        ShowStatus("Edit cancelled.");
    }

    private void AddRow(SampleDto sample)
    {
        var row = new SampleRow
        {
            Id = sample.Id,
            Name = sample.Name,
            Status = sample.Status
        };

        row.SelectionChanged += (_, _) => RefreshSelectionState();
        _samples.Add(row);
    }

    private bool TryReadForm(out int id, out string name, out string status)
    {
        name = NameBox.Text.Trim();
        status = StatusCombo.SelectedItem as string ?? "";
        id = 0;

        if (!int.TryParse(IdBox.Text.Trim(), out id) || id <= 0)
        {
            ShowStatus("Id must be a positive whole number.", isError: true);
            return false;
        }

        if (name.Length == 0)
        {
            ShowStatus("Name is required.", isError: true);
            return false;
        }

        if (status.Length == 0)
        {
            ShowStatus("Choose a status.", isError: true);
            return false;
        }

        return true;
    }

    private SampleRow? LastCheckedRow()
    {
        return _samples
            .Where(sample => sample.IsSelected)
            .OrderBy(sample => sample.SelectionOrder)
            .LastOrDefault();
    }

    private int SuggestNextId()
    {
        if (_samples.Count == 0)
        {
            return 1;
        }

        return _samples.Max(sample => sample.Id) + 1;
    }

    private void RefreshSelectionState()
    {
        if (_isBusy)
        {
            return;
        }

        var selected = _samples.Where(sample => sample.IsSelected).ToList();
        ModifyButton.IsEnabled = selected.Count > 0;
        DeleteButton.IsEnabled = selected.Count > 0;

        if (selected.Count == 0)
        {
            SelectionHint.Text = "Check one or more rows to modify or delete.";
        }
        else if (selected.Count == 1)
        {
            SelectionHint.Text = "1 selected. Modify opens that sample.";
        }
        else
        {
            var lastId = selected.OrderBy(sample => sample.SelectionOrder).Last().Id;
            SelectionHint.Text = $"{selected.Count} selected. Modify opens the last one you checked (id {lastId}).";
        }
    }

    private void UpdateCount()
    {
        CountText.Text = _samples.Count == 1 ? "1 sample" : $"{_samples.Count} samples";
    }

    private void HideEditor()
    {
        EditorPanel.Visibility = Visibility.Collapsed;
        _isEditing = false;
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        AddButton.IsEnabled = !isBusy;
        GenerateButton.IsEnabled = !isBusy;
        SaveButton.IsEnabled = !isBusy;
        CancelButton.IsEnabled = !isBusy;
        RefreshButton.IsEnabled = !isBusy && _refreshReady;

        if (isBusy)
        {
            ModifyButton.IsEnabled = false;
            DeleteButton.IsEnabled = false;
            return;
        }

        RefreshSelectionState();
    }

    private void ShowStatus(string message, bool isError = false)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? new SolidColorBrush(Color.FromRgb(155, 58, 50))
            : new SolidColorBrush(Color.FromRgb(28, 51, 48));
    }
}
