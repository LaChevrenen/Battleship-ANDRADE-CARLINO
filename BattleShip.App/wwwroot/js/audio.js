// Audio du jeu, entièrement synthétisé : aucune ressource externe, aucune licence à porter.
// Deux bus séparés, les effets courts d'un côté et la musique de l'autre, pour que couper l'un
// ne touche pas l'autre.

let audioContext;
let sfxBus;
let musicBus;
let padFilter;
let musicTimer;
let musicBar = 0;
let nextBarTime = 0;
let mood = 'calm';

const MUSIC_LEVEL = 0.62;

// Préparation et accueil : nappes tenues, longues, rien qui pousse.
const CALM_BARS = 9;
const CALM_CHORDS = [
    [146.83, 185.00, 220.00, 277.18, 329.63],
    [123.47, 146.83, 185.00, 220.00, 293.66],
    [98.00, 123.47, 146.83, 185.00, 246.94],
    [110.00, 146.83, 164.81, 196.00, 246.94],
];

// Combat : 96 à la noire, huit temps par mesure, donc une mesure de 5 secondes pile.
const BEAT = 60 / 96;
const COMBAT_BARS = BEAT * 8;

// Ré mineur, i - VI - III - VII : la marche qui sonne épique. Chaque accord donne sa fondamentale,
// sa tierce, sa quinte et son octave, dont on se sert différemment selon la voix.
const COMBAT_CHORDS = [
    { root: 146.83, third: 174.61, fifth: 220.00, octave: 293.66 },
    { root: 116.54, third: 146.83, fifth: 174.61, octave: 233.08 },
    { root: 174.61, third: 220.00, fifth: 261.63, octave: 349.23 },
    { root: 130.81, third: 164.81, fifth: 196.00, octave: 261.63 },
];

const audio = () => {
    const AudioContext = window.AudioContext || window.webkitAudioContext;
    if (!AudioContext) return null;

    if (!audioContext) {
        audioContext = new AudioContext();

        // Compresseur sur la sortie : nappes, pulsation et effet peuvent tomber ensemble, et la
        // somme saturerait sans lui.
        const master = audioContext.createDynamicsCompressor();
        master.threshold.value = -14;
        master.knee.value = 24;
        master.ratio.value = 8;
        master.attack.value = 0.004;
        master.release.value = 0.22;
        master.connect(audioContext.destination);

        sfxBus = audioContext.createGain();
        sfxBus.gain.value = 0.6;
        sfxBus.connect(master);

        // Réverbération pauvre mais convaincante : un écho court et sombre réinjecté sur lui-même.
        // Un vrai convolver demanderait une réponse impulsionnelle à embarquer.
        const delay = audioContext.createDelay(1);
        delay.delayTime.value = 0.38;
        const feedback = audioContext.createGain();
        feedback.gain.value = 0.3;
        const damping = audioContext.createBiquadFilter();
        damping.type = 'lowpass';
        damping.frequency.value = 1800;
        const wet = audioContext.createGain();
        wet.gain.value = 0.32;

        musicBus = audioContext.createGain();
        musicBus.gain.value = MUSIC_LEVEL;

        // Coupe-bas sur toute la musique : sous 34 Hz on n'entend rien, mais cette énergie mange
        // la marge et fait crépiter le grave dès qu'une frappe s'ajoute au bourdon.
        const rumble = audioContext.createBiquadFilter();
        rumble.type = 'highpass';
        rumble.frequency.value = 34;
        rumble.Q.value = 0.7;

        musicBus.connect(rumble);
        rumble.connect(master);
        rumble.connect(delay);
        delay.connect(damping).connect(feedback).connect(delay);
        damping.connect(wet).connect(master);

        // Filtre réservé aux nappes calmes : le combat ne passe pas par lui, sinon il serait
        // étouffé et sonnerait comme le lobby.
        padFilter = audioContext.createBiquadFilter();
        padFilter.type = 'lowpass';
        padFilter.frequency.value = 1150;
        padFilter.Q.value = 0.7;
        padFilter.connect(musicBus);

        const breath = audioContext.createOscillator();
        const breathDepth = audioContext.createGain();
        breath.frequency.value = 0.045;
        breathDepth.gain.value = 380;
        breath.connect(breathDepth).connect(padFilter.frequency);
        breath.start();
    }

    if (audioContext.state === 'suspended') audioContext.resume();
    return audioContext;
};

const noise = (context, start, duration, volume) => {
    const buffer = context.createBuffer(1, context.sampleRate * duration, context.sampleRate);
    const data = buffer.getChannelData(0);
    for (let index = 0; index < data.length; index++) data[index] = Math.random() * 2 - 1;
    const source = context.createBufferSource();
    const gain = context.createGain();
    source.buffer = buffer;
    gain.gain.setValueAtTime(volume, start);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
    source.connect(gain).connect(sfxBus);
    source.start(start);
};

const tone = (context, frequency, start, duration, volume, type = 'triangle') => {
    const oscillator = context.createOscillator();
    const gain = context.createGain();
    oscillator.frequency.value = frequency;
    oscillator.type = type;
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(volume, start + 0.025);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);
    oscillator.connect(gain).connect(sfxBus);
    oscillator.start(start);
    oscillator.stop(start + duration + 0.03);
};

// Sons d'interface, un par intention plutôt que par apparence : deux boutons "ghost" avec des
// rôles différents (annuler / placer au hasard) ne sonnent pas pareil, et inversement deux
// boutons de styles différents qui font "la même chose" (valider) partagent leur son. La classe
// CSS choisit une apparence, l'attribut data-sound choisit une intention ; les deux peuvent
// diverger, donc ils sont posés séparément dans le balisage.
const UI_SOUNDS = {
    primary: { notes: [523.25, 659.25], type: 'triangle', duration: 0.16, volume: 0.34, stagger: 0.055 },
    secondary: { notes: [392.00], type: 'sine', duration: 0.14, volume: 0.22 },
    destructive: { notes: [392.00, 293.66], type: 'sawtooth', duration: 0.12, volume: 0.2, stagger: 0.05, noiseDuration: 0.08, noiseVolume: 0.1 },
    'toggle-on': { notes: [440, 659.25], type: 'square', duration: 0.07, volume: 0.15, stagger: 0.045 },
    'toggle-off': { notes: [440, 293.66], type: 'square', duration: 0.07, volume: 0.15, stagger: 0.045 },
    'stepper-up': { notes: [880], type: 'square', duration: 0.045, volume: 0.13 },
    'stepper-down': { notes: [277.18], type: 'square', duration: 0.045, volume: 0.13 },
    pick: { notes: [587.33, 739.99], type: 'triangle', duration: 0.09, volume: 0.24, stagger: 0.04 },
    nav: { notes: [329.63], type: 'sine', duration: 0.16, volume: 0.14, noiseDuration: 0.16, noiseVolume: 0.07 },
    'panel-open': { notes: [392.00, 523.25, 659.25], type: 'triangle', duration: 0.1, volume: 0.22, stagger: 0.045 },
    'panel-close': { notes: [659.25, 523.25, 392.00], type: 'triangle', duration: 0.09, volume: 0.18, stagger: 0.04 },
    checkbox: { notes: [523.25], type: 'sine', duration: 0.06, volume: 0.12 },
    select: { notes: [700, 900], type: 'triangle', duration: 0.09, volume: 0.2, stagger: 0.05 },
};

const playSound = (context, start, sound) => {
    sound.notes.forEach((frequency, index) =>
        tone(context, frequency, start + index * (sound.stagger ?? 0), sound.duration, sound.volume, sound.type));

    if (sound.noiseDuration)
        noise(context, start, sound.noiseDuration, sound.noiseVolume ?? 0.1);
};

// Issue d'un tir ou d'une partie. miss/hit/sunk reproduisent exactement les réglages d'avant les
// sons d'interface ci-dessus (mêmes fréquences, mêmes durées) ; seuls start/win/lose sont
// nouveaux ou enrichis, cf. Partie.razor pour la séquence de début de partie qui déclenche "start".
const GAME_SOUNDS = {
    miss: { notes: [220], type: 'triangle', duration: 0.36, volume: 0.38, noiseDuration: 0.12, noiseVolume: 0.22 },
    hit: { notes: [440, 554, 659], type: 'triangle', duration: 0.36, volume: 0.38, stagger: 0.07, noiseDuration: 0.12, noiseVolume: 0.22 },
    sunk: { notes: [220, 165, 110], type: 'triangle', duration: 0.36, volume: 0.48, stagger: 0.07, noiseDuration: 0.42, noiseVolume: 0.34 },
    // Montée sur cinq notes, calée pour tenir dans l'animation "Combat engagé !" (~1,1 s) sans être coupée.
    start: { notes: [261.63, 329.63, 392.00, 523.25, 659.25], type: 'triangle', duration: 0.3, volume: 0.4, stagger: 0.1 },
    // Même geste ascendant que "start", complété d'une octave de résolution et d'un volume plus large.
    win: { notes: [523.25, 659.25, 783.99, 1046.50, 1318.51], type: 'triangle', duration: 0.42, volume: 0.42, stagger: 0.095, noiseDuration: 0.12, noiseVolume: 0.22 },
    // Descente à l'inverse de "win", avec la traîne de bruit la plus longue après "sunk" : toute
    // la flotte est perdue, pas un seul navire.
    lose: { notes: [392.00, 349.23, 293.66, 220.00, 164.81], type: 'triangle', duration: 0.4, volume: 0.4, stagger: 0.1, noiseDuration: 0.3, noiseVolume: 0.26 },
};

window.battleSound = (kind) => {
    const context = audio();
    if (!context) return;

    const start = context.currentTime;
    const sound = UI_SOUNDS[kind] ?? GAME_SOUNDS[kind];
    if (sound) playSound(context, start, sound);
};

// --- Ambiance calme -------------------------------------------------------------------------

// Deux oscillateurs légèrement désaccordés, entrée et sortie très longues. C'est le recouvrement
// entre deux accords qui fait qu'on n'entend aucune coupure.
const pad = (context, frequency, start, volume) => {
    [-6, 6].forEach(detune => {
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        oscillator.type = 'sine';
        oscillator.frequency.value = frequency;
        oscillator.detune.value = detune;
        gain.gain.setValueAtTime(0.0001, start);
        gain.gain.linearRampToValueAtTime(volume, start + 3.4);
        gain.gain.setValueAtTime(volume, start + CALM_BARS - 1.5);
        gain.gain.linearRampToValueAtTime(0.0001, start + CALM_BARS + 3);
        oscillator.connect(gain).connect(padFilter);
        oscillator.start(start);
        oscillator.stop(start + CALM_BARS + 3.2);
    });
};

const scheduleCalm = (context, start) => {
    const chord = CALM_CHORDS[musicBar % CALM_CHORDS.length];
    chord.forEach((frequency, index) => pad(context, frequency, start, index === 0 ? 0.14 : 0.08));
    pad(context, chord[0] / 2, start, 0.11);

    // Une note isolée un accord sur deux : assez pour que ça vive, trop rare pour devenir une
    // mélodie qu'on finit par attendre.
    if (musicBar % 2 === 1) {
        const oscillator = context.createOscillator();
        const gain = context.createGain();
        oscillator.type = 'triangle';
        oscillator.frequency.value = chord[3] * 2;
        gain.gain.setValueAtTime(0.0001, start + 1.5);
        gain.gain.linearRampToValueAtTime(0.055, start + 2);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + CALM_BARS - 1);
        oscillator.connect(gain).connect(padFilter);
        oscillator.start(start + 1.5);
        oscillator.stop(start + CALM_BARS - 0.8);
    }
};

// --- Ambiance de combat ---------------------------------------------------------------------

// Voix mordante : dent de scie et filtre résonant qui se referme. C'est ce timbre, et pas les
// notes, qui distingue le combat des nappes.
const brass = (context, frequency, start, duration, volume, cutoff) => {
    const oscillator = context.createOscillator();
    const filter = context.createBiquadFilter();
    const gain = context.createGain();

    oscillator.type = 'sawtooth';
    oscillator.frequency.value = frequency;

    filter.type = 'lowpass';
    filter.Q.value = 7;
    filter.frequency.setValueAtTime(cutoff * 2.6, start);
    filter.frequency.exponentialRampToValueAtTime(cutoff, start + duration * 0.7);

    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.linearRampToValueAtTime(volume, start + 0.018);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);

    oscillator.connect(filter).connect(gain).connect(musicBus);
    oscillator.start(start);
    oscillator.stop(start + duration + 0.05);
};

// Grosse caisse : sinus qui plonge, filtré et volontairement modeste. C'est elle qui saturait.
const kick = (context, start, volume = 0.26) => {
    const oscillator = context.createOscillator();
    const filter = context.createBiquadFilter();
    const gain = context.createGain();

    oscillator.type = 'sine';
    oscillator.frequency.setValueAtTime(125, start);
    oscillator.frequency.exponentialRampToValueAtTime(44, start + 0.13);

    filter.type = 'lowpass';
    filter.frequency.value = 170;

    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.linearRampToValueAtTime(volume, start + 0.014);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + 0.36);

    oscillator.connect(filter).connect(gain).connect(musicBus);
    oscillator.start(start);
    oscillator.stop(start + 0.38);
};

// Bruit filtré : la caisse claire garde le médium, le charleston ne garde que l'aigu. Les deux
// sont coupés dans le grave, sinon chaque frappe rajouterait de l'énergie là où ça sature.
const hit = (context, start, duration, volume, cutoff, type) => {
    const buffer = context.createBuffer(1, context.sampleRate * duration, context.sampleRate);
    const data = buffer.getChannelData(0);
    for (let index = 0; index < data.length; index++) data[index] = Math.random() * 2 - 1;

    const source = context.createBufferSource();
    const filter = context.createBiquadFilter();
    const gain = context.createGain();

    source.buffer = buffer;
    filter.type = type;
    filter.frequency.value = cutoff;

    gain.gain.setValueAtTime(volume, start);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + duration);

    source.connect(filter).connect(gain).connect(musicBus);
    source.start(start);
};

const snare = (context, start, volume = 0.16) => hit(context, start, 0.18, volume, 1400, 'highpass');

const hat = (context, start, volume) => hit(context, start, 0.045, volume, 7000, 'highpass');

const scheduleCombat = (context, start) => {
    const chord = COMBAT_CHORDS[musicBar % COMBAT_CHORDS.length];

    // Bourdon grave tenu sous toute la mesure : l'assise. En triangle et non en dent de scie —
    // à cette hauteur, les harmoniques d'une dent de scie empâtent le grave et le font crépiter.
    const drone = context.createOscillator();
    const droneGain = context.createGain();
    drone.type = 'triangle';
    drone.frequency.value = chord.root / 2;
    droneGain.gain.setValueAtTime(0.0001, start);
    droneGain.gain.linearRampToValueAtTime(0.07, start + 0.4);
    droneGain.gain.setValueAtTime(0.07, start + COMBAT_BARS - 0.5);
    droneGain.gain.linearRampToValueAtTime(0.0001, start + COMBAT_BARS);
    drone.connect(droneGain).connect(musicBus);
    drone.start(start);
    drone.stop(start + COMBAT_BARS + 0.05);

    // Ostinato de croches : c'est lui qui donne l'urgence, et il ne s'arrête jamais.
    const pattern = [0, 2, 0, 3, 0, 2, 0, 2];
    const degrees = [chord.root, chord.third, chord.fifth, chord.octave];
    for (let eighth = 0; eighth < 16; eighth++) {
        const when = start + eighth * BEAT / 2;
        brass(context, degrees[pattern[eighth % pattern.length]], when, 0.16, 0.13, 900);
    }

    // Accords appuyés sur les temps forts.
    [0, 4].forEach(beat => {
        const when = start + beat * BEAT;
        [chord.root * 2, chord.third * 2, chord.fifth * 2].forEach(frequency =>
            brass(context, frequency, when, BEAT * 1.6, 0.075, 1500));
    });

    // Batterie. La grosse caisse pose les appuis et syncope une fois par moitié de mesure, la
    // caisse claire répond sur les temps 3 et 7, et le charleston tient les croches.
    [0, 2, 3.5, 4, 6, 7.5].forEach(beat => kick(context, start + beat * BEAT, beat % 1 === 0 ? 0.26 : 0.18));
    [2, 6].forEach(beat => snare(context, start + beat * BEAT));
    snare(context, start + 5.5 * BEAT, 0.07);

    for (let eighth = 0; eighth < 16; eighth++)
        hat(context, start + eighth * BEAT / 2, eighth % 2 === 0 ? 0.05 : 0.03);

    // Toutes les quatre mesures, une relance en doubles croches annonce le retour du cycle.
    if (musicBar % 4 === 3)
        for (let sixteenth = 0; sixteenth < 4; sixteenth++)
            snare(context, start + 7 * BEAT + sixteenth * BEAT / 4, 0.09 + sixteenth * 0.03);
};

// --- Ordonnancement -------------------------------------------------------------------------

const barSeconds = () => (mood === 'combat' ? COMBAT_BARS : CALM_BARS);

// Chaque mesure est posée à l'avance sur l'horloge audio, et non au moment où le minuteur tombe :
// sans ça, la dérive de setInterval s'entendrait à chaque mesure sur un rythme.
const lookahead = () => {
    const context = audio();
    if (!context) return;

    while (nextBarTime < context.currentTime + 0.4) {
        const start = Math.max(nextBarTime, context.currentTime + 0.05);
        if (mood === 'combat') scheduleCombat(context, start);
        else scheduleCalm(context, start);

        musicBar++;
        nextBarTime = start + barSeconds();
    }
};

const startBars = (context) => {
    clearInterval(musicTimer);
    nextBarTime = context.currentTime + 0.08;
    lookahead();
    musicTimer = setInterval(lookahead, 120);
};

window.battleMusic = (enabled) => {
    if (!enabled) {
        clearInterval(musicTimer);
        musicTimer = null;

        // Fondu de sortie : couper net ferait claquer les voix en cours.
        if (musicBus && audioContext) {
            const now = audioContext.currentTime;
            musicBus.gain.cancelScheduledValues(now);
            musicBus.gain.setValueAtTime(musicBus.gain.value, now);
            musicBus.gain.linearRampToValueAtTime(0.0001, now + 1.2);
        }

        return;
    }

    const context = audio();
    if (!context) return;

    const now = context.currentTime;
    musicBus.gain.cancelScheduledValues(now);
    musicBus.gain.setValueAtTime(Math.max(musicBus.gain.value, 0.0001), now);
    musicBus.gain.linearRampToValueAtTime(MUSIC_LEVEL, now + 2);

    if (musicTimer) return;
    startBars(context);
};

// Passage d'une ambiance à l'autre. Les nappes calmes déjà lancées gardent leur longue extinction,
// donc le combat démarre par-dessus sans coupure.
window.battleMusicMood = (next) => {
    if ((next !== 'calm' && next !== 'combat') || next === mood) return;

    mood = next;
    musicBar = 0;
    if (!audioContext || !musicTimer) return;

    startBars(audioContext);
};

// Le navigateur interdit de produire du son avant un geste de l'utilisateur. On tente au
// chargement, et si le contexte reste suspendu, le premier geste le réveille.
window.battleAudioStart = () => {
    window.battleMusic(true);
    if (!audioContext || audioContext.state !== 'suspended') return;

    const events = ['pointerdown', 'keydown', 'touchstart'];
    const wake = () => {
        audioContext.resume();
        window.battleMusic(true);
        events.forEach(event => document.removeEventListener(event, wake));
    };

    events.forEach(event => document.addEventListener(event, wake));
};

// Chaque élément qui doit sonner porte son intention dans data-sound (posé dans le balisage) :
// pas de son par défaut pour un bouton non catégorisé, plutôt qu'un son générique qui masquerait
// un oubli de catégorisation.
document.addEventListener('click', event => {
    const target = event.target.closest('[data-sound]');
    if (target) window.battleSound(target.dataset.sound);
});
