# Elrond TradeOffer

## Description

The goal of this projects is to allow you to safely exchange tokens between you and another party. 
For this purpose a smart contract will be used and act as a trustee that performs the exchange.

### Basic workflow

1. A seller will make an offer ("I want to sell 100k MEX")
2. Multiple parties can place bids ("I bid 0.5 EGLD", "I give you 150k LKMEX")
3. The seller accepts one of the bids and both parties come to an agreement.
4. The seller initiates the trade and sends the offered tokens to the smart contract. The buyer gets notified.
5. The buyer sends the bid tokens to the smart contract.
6. The smart contract sends back the traded tokens to each party.

The catch here is that the smart contract will only release the tokens, if both parties send the exact number of tokens that both parties have agreed on.
This way both the seller and the buyer are protected from being scammed.

## Components

This repository contains two components:

### [src/bot/Elrond.TradeOffer.Web](https://github.com/janniksam/elrond-tradeoffer/tree/main/src/bot/Elrond.TradeOffer.Web)

This is a telegram bot, that interacts with our smart contract. It acts as our user interface and is written in C# / hosted in a .NET 6 ASP.NET application.

### [src/sc/elrond-tradoffer](https://github.com/janniksam/elrond-tradeoffer/tree/main/src/sc/elrondtradeoffer)

This is the smart contract written in RUST which plays the role of the trustee between two partys. All funds are being held by the smart contract, until either 
- the bidding party is sending their tokens to the SC or...
- ... you cancel the offer you initiated.
