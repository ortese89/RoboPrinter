using System.Timers;

namespace Utilities.core;

public class TimedBit : IDisposable
{
    private Action<TimedBit> _Callback { get; set; } = null;
    private System.Timers.Timer _ContDown { get; set; } = new();
    private object _Structure { get; set; } = null;

    public string Name { get; set; } = null;
    private bool Value { get; set; } = false;

    public TimedBit(object structure, string bitName, int delay, bool setValue = true, Action<TimedBit> callback = null)
    {
        _Structure = structure;
        Name = bitName;
        _ContDown.Interval = delay;
        _Callback = callback;
        Value = setValue;
        _Structure.GetType().GetProperty(Name).SetValue(_Structure, Value);
        Value = !Value;
        _ContDown.Elapsed += new ElapsedEventHandler(ElapsedDelay);
        _ContDown.Enabled = true;
    }

    private void ElapsedDelay(object sender, ElapsedEventArgs e)
    {
        _Structure.GetType().GetProperty(Name).SetValue(_Structure, Value);

        if (_Callback != null)
            _Callback.Invoke(this);

        this.Dispose(true);
    }

    private bool disposedValue; // Per rilevare chiamate ridondanti

    // IDisposable
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
            }
        }
        disposedValue = true;
    }

    // TODO: eseguire l'override di Finalize() solo se Dispose(disposing As Boolean) include il codice per liberare risorse non gestite.
    // Protected Overrides Sub Finalize()
    // ' Non modificare questo codice. Inserire sopra il codice di pulizia in Dispose(disposing As Boolean).
    // Dispose(False)
    // MyBase.Finalize()
    // End Sub

    // Questo codice viene aggiunto da Visual Basic per implementare in modo corretto il criterio Disposable.
    public void Dispose()
    {
        // Non modificare questo codice. Inserire sopra il codice di pulizia in Dispose(disposing As Boolean).
        Dispose(true);
    }
}
