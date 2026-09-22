using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LabSampleTracker.Desktop.Models;

/// <summary>
/// One grid row. SelectionOrder goes up each time the box is checked,
/// so Modify can open the last row the user checked.
/// </summary>
public class SampleRow : INotifyPropertyChanged
{
    private static int _selectionSequence;

    private string _name = "";
    private string _status = "";
    private bool _isSelected;

    public int Id { get; set; }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
        }
    }

    public int SelectionOrder { get; private set; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            if (_isSelected)
            {
                SelectionOrder = ++_selectionSequence;
            }

            OnPropertyChanged();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? SelectionChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
