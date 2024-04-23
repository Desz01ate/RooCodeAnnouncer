package events

import (
	"deszolate/announcer/structs"
)

type NewCodeNotification struct {
	Code  string
	Items []structs.Reward
}
