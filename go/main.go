package main

import (
	"context"
	"fmt"
	"os"
	"os/signal"
	"syscall"
	"time"

	"deszolate/announcer/events"
	"deszolate/announcer/handlers"
	"deszolate/announcer/structs"

	"github.com/go-co-op/gocron/v2"
	"github.com/mehdihadeli/go-mediatr"
)

func main() {
	fmt.Println("Initializing code announcer...")

	discordPublisher := &handlers.DiscordNotificationHandler{}
	linePublisher := &handlers.LineNotificationHandler{}

	mediatr.RegisterNotificationHandlers(discordPublisher, linePublisher)

	scheduler, err := gocron.NewScheduler()

	if err != nil {
		panic(err)
	}

	duration := gocron.DurationJob(time.Second * 5)
	task := gocron.NewTask(func() {
		ch := make(chan structs.ItemCode)
		go readCode(ch)

		ctx := context.TODO()
		for item := range ch {
			event := &events.NewCodeNotification{
				Code:  item.Code,
				Items: item.Rewards,
			}
			mediatr.Publish(ctx, event)
		}
	})
	job, err := scheduler.NewJob(duration, task)

	if err != nil {
		panic(err)
	}

	scheduler.Start()

	c := make(chan os.Signal, 1)
	signal.Notify(c, os.Interrupt, syscall.SIGTERM)
	go func() {
		<-c
		fmt.Println("Stopped.")
		scheduler.Shutdown()
		os.Exit(0)
	}()

	fmt.Println("Code announcer started with job id " + job.ID().String())
	for {
		time.Sleep(10 * time.Second)
	}
}
