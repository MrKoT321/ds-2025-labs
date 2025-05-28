Feature: Valuator pages
    Scenario: User submits text and sees similarity and rank on summary page
        Given user opens the Index page
        When user submits the text "good text why not"
        Then user is redirected to the Summary page
        And Similarity should be "1"
        And Rank should be "0.1765"