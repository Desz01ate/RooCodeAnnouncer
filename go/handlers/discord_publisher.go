package handlers

import (
	"context"
	"deszolate/announcer/events"
	"fmt"
)

type DiscordNotificationHandler struct {
}

func (c *DiscordNotificationHandler) Handle(ctx context.Context, event *events.NewCodeNotification) error {
	//Do something with the event here !
	fmt.Printf("discord handler invoked with %s\n", event.Code)
	return nil
}
