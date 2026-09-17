using Microsoft.JSInterop;

namespace BattleShip.App.Services;

// Réglages audio de l'application entière. Enregistré une fois pour toute la session : la musique
// ne doit ni s'arrêter ni se réinitialiser quand on passe de l'accueil à une partie.
public sealed class GameAudio(IJSRuntime js)
{
    private bool started;
    private string mood = "calm";

    public bool SoundEnabled { get; private set; } = true;
    public bool MusicEnabled { get; private set; } = true;

    // Démarrage au premier rendu de la mise en page. Le navigateur peut refuser tant que
    // l'utilisateur n'a rien touché : c'est le script qui guette alors son premier geste.
    public async Task StartAsync()
    {
        if (started)
            return;

        started = true;
        if (MusicEnabled)
            await js.InvokeVoidAsync("battleAudioStart");
    }

    public async Task SetMusicAsync(bool enabled)
    {
        MusicEnabled = enabled;

        if (enabled)
            await js.InvokeVoidAsync("battleAudioStart");
        else
            await js.InvokeVoidAsync("battleMusic", false);
    }

    public void SetSound(bool enabled) => SoundEnabled = enabled;

    // Appelée à chaque rendu : on ne traverse l'interop que si l'ambiance change réellement.
    public async Task SetMoodAsync(string next)
    {
        if (mood == next)
            return;

        mood = next;
        await js.InvokeVoidAsync("battleMusicMood", next);
    }

    public async Task PlayAsync(string kind)
    {
        if (SoundEnabled)
            await js.InvokeVoidAsync("battleSound", kind);
    }
}
