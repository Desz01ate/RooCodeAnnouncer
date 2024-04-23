package main

import (
	"bytes"
	"deszolate/announcer/structs"
	"fmt"
	"regexp"
	"strconv"
	"strings"

	"github.com/antchfx/htmlquery"
	"golang.org/x/net/html"
)

func readCode(ch chan structs.ItemCode) {
	defer close(ch)

	doc, err := htmlquery.LoadURL("https://gamingph.com/2023/04/redeem-codes-for-ragnarok-origin-roo-guide/")
	if err != nil {
		fmt.Println(err.Error())

		return
	}

	rows := htmlquery.Find(doc, "//tr")

	for index, row := range rows {
		// skip header row
		if index == 0 {
			continue
		}

		children := []*html.Node{}
		for current := row.FirstChild; current != nil; current = current.NextSibling {
			children = append(children, current)
		}

		if len(children) != 2 {
			continue
		}

		left := children[0]
		right := children[1]

		code, isNew := parseItemCode(left)
		rewards := parseRewards(right)

		if isNew {
			ch <- structs.ItemCode{
				Code:       code,
				RawRewards: htmlquery.InnerText(right),
				Rewards:    rewards,
			}
		}
	}
}

func parseItemCode(node *html.Node) (string, bool) {
	buffer := bytes.Buffer{}
	isNew := false

	for current := node.FirstChild; current != nil; current = current.NextSibling {
		text := htmlquery.InnerText(current)

		hasForbiddenKeyword := containsForbiddenKeyword(text)
		isUnderBracket := isUnderBracket(text)

		if hasForbiddenKeyword || isUnderBracket {
			isNew = isNew || strings.Contains(strings.ToLower(text), "new code")
			continue
		}

		buffer.WriteString(text)
	}

	return buffer.String(), isNew
}

func containsForbiddenKeyword(text string) bool {
	forbiddenKeywords := []string{
		"&nbsp", "new code",
	}
	for _, kw := range forbiddenKeywords {
		if strings.Contains(strings.ToLower(text), strings.ToLower(kw)) {
			return true
		}
	}
	return false
}

func isUnderBracket(text string) bool {
	return strings.HasPrefix(text, "(") && strings.HasSuffix(text, ")")
}

var splitterRegex = regexp.MustCompile(`([\d,]+)\s([x]{1}\s+|\s*)([^x\s].+)`)
var imageRegex = regexp.MustCompile(`<img[^>]*>`)

func parseRewards(node *html.Node) []structs.Reward {
	rewards := []structs.Reward{}
	buffer := bytes.Buffer{}
	for current := node.FirstChild; current != nil; current = current.NextSibling {
		text := htmlquery.InnerText(current)
		text = strings.Replace(text, "\u00a0", " ", -1)
		text = imageRegex.ReplaceAllString(text, "")

		if text == "" {
			continue
		}

		buffer.WriteString(text)
		computingText := buffer.String()

		// append to the buffer the regex is satisfied.
		if !splitterRegex.MatchString(computingText) {
			continue
		}

		groups := splitterRegex.FindStringSubmatch(computingText)

		qtyStr := strings.Replace(groups[1], ",", "", -1)
		qty, err := strconv.Atoi(qtyStr)
		if err != nil {
			qty = 1
		}

		name := groups[len(groups)-1]

		reward := structs.Reward{
			Name:     name,
			Quantity: qty,
		}
		rewards = append(rewards, reward)

		buffer.Reset()

	}

	return rewards
}
