package handlers

import (
	"context"
	"deszolate/announcer/events"
	"fmt"
)

type LineNotificationHandler struct {
}

func (c *LineNotificationHandler) Handle(ctx context.Context, event *events.NewCodeNotification) error {
	//Do something with the event here !
	fmt.Printf("line handler invoked with %s\n", event.Code)
	fmt.Println(event.Items)
	return nil
}
