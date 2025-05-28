Feature: Rank calculation processing

  Scenario: Message received with text only with alphabet symbols
    Given a text with ID "123" exists with content "text"
    When a message with body "123" is received
    Then the rank should be calculated and stored with ID "123" and Value "0"
    And an event should be published to RabbitMQ with routing key "RankCalculated"
    And the SignalR hub should broadcast "RankCalculated" with the correct rank

  Scenario: Message received with text without alphabet symbols
    Given a text with ID "321" exists with content "|&!@#)"
    When a message with body "321" is received
    Then the rank should be calculated and stored with ID "321" and Value "1"
    And an event should be published to RabbitMQ with routing key "RankCalculated"
    And the SignalR hub should broadcast "RankCalculated" with the correct rank
