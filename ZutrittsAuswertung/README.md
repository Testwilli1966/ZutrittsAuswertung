# Zutritts-Auswertung (Home-Assistant-Add-on)

Kleine, ressourcenschonende Web-Anwendung (ASP.NET Core Razor Pages, .NET 8),
die die beiden SQLite-Datenbanken des myaxxess/FEIG-Zutrittssystems
zusammenführt und eine filterbare Auswertungsmaske für Zutritte bereitstellt:

- `members.sqlite3` – Mitgliederstammdaten (Beitragsstatus, Lexware-Abgleich, 10er-Karte, ...)
- `access_history.sqlite3` – Zutritts-Log (ein Eintrag pro Türöffnungsversuch)

Beide Dateien liegen bei dir unter `/share/FEIG_MAX50/...` – also im normalen
Home-Assistant-`/share`-Ordner. Deshalb läuft diese App als **lokales
Home-Assistant-Add-on** (Docker-Container), das `/share` direkt lokal
mountet – kein Netzwerk, kein Passwort, kein SMB-Locking-Risiko.

**Wichtiger Hinweis:** Der C#-Code wurde bereits erfolgreich mit `dotnet build`
getestet (siehe Chatverlauf). Der Docker-Build selbst (dieser Schritt läuft
erst auf deinem Pi über den Supervisor) konnte in der Umgebung, in der dieses
Add-on erstellt wurde, nicht ausgeführt werden (kein Internetzugriff auf
Docker-Registries). Bitte beim ersten Start einmal das Add-on-Log prüfen.

## 1. Add-on auf den Pi kopieren

Kopiere den kompletten Ordner `ZutrittsAuswertung` (mit `config.yaml`,
`Dockerfile`, den `.cs`/`.cshtml`-Dateien usw.) in den `addons`-Ordner deines
Home-Assistant-Systems, z. B.:

- über die **Samba-Freigabe „addons"** (falls im Samba-Add-on aktiviert;
  gleicher Weg wie bei `share`, nur eben die Freigabe `addons` statt `share`)
- oder über die SSH/Terminal-Verbindung, die du vorhin schon für die
  `docker exec`-Befehle genutzt hast, z. B. per `scp`

Ergebnis sollte sein, dass auf dem Pi z. B. folgender Pfad existiert:
`/addons/ZutrittsAuswertung/config.yaml`

## 2. Add-on installieren

1. In Home Assistant: **Einstellungen → Add-ons → Add-on Store**
2. Oben rechts über das 3-Punkte-Menü **„Repositories aktualisieren"** bzw.
   die Seite neu laden – das Add-on sollte danach unter **„Lokale Add-ons"**
   auftauchen (Name: „Zutritts-Auswertung")
3. Öffnen → **Installieren** (das baut das Docker-Image auf dem Pi – das
   kann beim ersten Mal ein paar Minuten dauern, da Basis-Images geladen
   werden müssen)
4. Nach erfolgreicher Installation: **Starten**
5. Im Tab **„Log"** prüfen, ob die App sauber hochfährt (keine Fehler beim
   Öffnen der beiden Datenbanken)

## 3. Aufrufen

Die Auswertungsmaske ist danach im lokalen Netz erreichbar unter:

```
http://<pi-adresse>:5080
```

## 4. Konfiguration

Die Pfade zu den beiden SQLite-Dateien stehen fest in `appsettings.json`
(container-intern, weil `/share` per `map: [share:ro]` in `config.yaml`
gemountet wird):

```json
"Datenbanken": {
  "MembersDb": "/share/FEIG_MAX50/database/members.sqlite3",
  "AccessHistoryDb": "/share/FEIG_MAX50/history/access_history.sqlite3"
}
```

Falls sich die Pfade auf dem Pi mal ändern, hier anpassen und das Add-on neu
bauen (Add-on-Seite → **Neu erstellen**/„Rebuild").

Die App öffnet beide Datenbanken **read-only** (zusätzlich ist `/share` im
Add-on ohnehin nur lesend gemountet, `share:ro`), damit sie den schreibenden
Prozess (das FEIG/myaxxess-Add-on) nicht blockiert.

## 5. Funktionsumfang (v1)

- Freitextsuche nach Name oder Kartennummer
- Zeitraumfilter (Von/Bis, Standard: heutiger Tag)
- Filter nach Status (alle / nur erlaubt / nur verweigert)
- Je Zutritt: Zeitpunkt, Name, Karte, Gruppe, Status, Beitragsstatus
  (inkl. offener Betrag), Auffälligkeits-Hinweis

## 6. Mögliche nächste Ausbaustufen

- Einbindung als **Ingress**-Seite (eigener Menüpunkt direkt in der
  HA-Seitenleiste statt IP:Port) – technisch etwas aufwändiger, da die App
  dann mit dem von Supervisor vorgegebenen Pfad-Präfix umgehen muss
- Besuchsstatistik pro Mitglied/Woche/Monat
- Export der gefilterten Liste (CSV)
- Hinweis auf Mitglieder mit abgelaufener 10er-Karte oder länger nicht da
- Login-Schutz, falls die App auch außerhalb des lokalen Netzes erreichbar
  sein soll (aktuell: keine Authentifizierung – nur fürs interne Netz gedacht)
