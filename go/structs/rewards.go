package structs

type Reward struct {
	Name     string
	Quantity int
}

type ItemCode struct {
	Code       string
	RawRewards string
	Rewards    []Reward
}
