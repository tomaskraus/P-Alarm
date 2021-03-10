
P-Alarm
-------


## Awareness alarm for Windows 
- calls an action if not turned off periodically
- audible alarm before the action call
- time intervals and action can be defined in the configuration file
- configurable text labels

## Usage

Run `P-Alarm.exe` 
To edit an action, action parameters, texts and duration, edit the `config.ini` file.

The small button under the Stop button sets the countdown time right before the call action. 
It can be used to speed up the app testing.

## configuration
```ini
; P-Alarm configuration file
;
[DURATIONS]
; time beteween alarm attempts
ALARM_PERIOD_MINS=20
; delay between showing the app window and calling an action
CALL_ACTION_DELAY_SECS=15
; how many seconds to beep before application calls an action
BEEP_COUNTDOWN_SECS=10

; how long to wait after the application called the action, without the stop button pressed
NOT_STOPPED_ALARM_PERIOD_MINS=5
; how many NOT_STOPPED_ALARM_PERIOD to wait before return to the ALARM_PERIOD_SECS
NOT_STOPPED_ALARM_PERIOD_REPEAT=2

; time between the second call of an action
; set to 0 to avoid the second call of the action
2ND_CALL_DELAY_SECS=10

[TEXTS]
ALARM_TEXT_DEFAULT= 
; the "$" character will be replaced with the actual countdown value when the application runs
ALARM_TEXT_COUNTDOWN=Zaènu volat sestru za: $ sekund
ALARM_TEXT_CALL=Volám sestru...

[ACTION]
; action: which app to run (include the full file path if necessary)
ALARM_ACTION_EXE="runscript.bat"
; app parameters
ALARM_ACTION_PARAMS=

```

## TODO
- GUI for the most used settings
- event log calls
- application icon
- installer?

