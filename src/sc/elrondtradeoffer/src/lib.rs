#![no_std]

elrond_wasm::imports!();

mod trade_offer;
use trade_offer::TradeOffer;

#[elrond_wasm::contract]

// A contract that plays the role of the middle man between to parties.
// You never have trusting someone to send you the tokens after you sent them your tokens.
// This SC allows anyone to trade tokens in a safe manner. 
pub trait Trader {

    #[init]
    fn init(
        &self
    ) -> SCResult<()> {
        Ok(())
    }

    // endpoints

    // This endpoint takes your tokens, that you wish to swap.
    // You also have to provide the following information:
    // -> wanna_have_token - What tokens do you want to receive?
    // -> wanna_have_amount - How much tokens do you want to receive?
    #[payable("*")]
    #[endpoint]
    fn offer(
        &self,
        #[payment_token] offer_token: TokenIdentifier,
        #[payment_amount] offer_amount: BigUint,
        #[payment_nonce] offer_nonce: u64,
        trade_offer_id: ManagedBuffer,
        wanna_have_token : TokenIdentifier,
        wanna_have_amount : BigUint,
        wanna_have_nonce: u64
    ) -> SCResult<()> {
        require!(
            wanna_have_amount > 0,
            "wanna_have_amount needs to be greater than 0"
        );

        require!(
            trade_offer_id.len() == 16,
            "trade_offer_id needs to be 16 bytes in length"
        );

        require!(
            self.trade_offer(&trade_offer_id).is_empty(),
            "An offer with this Id is already existing"
        );

        require!(
            self.finished_offer(&trade_offer_id).is_empty(),
            "An offer with this Id was already existing"
        );

        let caller = self.blockchain().get_caller();
        let offer = TradeOffer {
            offer_creator: caller,
            token_identifier_offered: offer_token,
            token_amount_offered: offer_amount,
            token_nonce_offered: offer_nonce,
            token_identifier_wanted: wanna_have_token,
            token_amount_wanted: wanna_have_amount,
            token_nonce_wanted: wanna_have_nonce
        };
        
        self.trade_offer(&trade_offer_id).set(&offer);

        Ok(())
    }
    
    // This endpoint cancels your trade offer and sends you back your tokens.
    #[endpoint]
    fn cancel_offer(
        &self,
        trade_offer_id: ManagedBuffer
    ) -> SCResult<()> {
        require!(
            !self.trade_offer(&trade_offer_id).is_empty(),
            "An offer with this name does not exist."
        );

        let caller = self.blockchain().get_caller();
        let info = self.trade_offer(&trade_offer_id).get();  
        require!(
            info.offer_creator == caller,
            "Only the creator of the offer can cancel the offer."
        );
        
        self.trade_offer(&trade_offer_id).clear();

        self.send()
            .direct(&caller, &info.token_identifier_offered, info.token_nonce_offered, &info.token_amount_offered, b"Trade offer cancelled");
    
        Ok(())
    }

    // This endpoint will be used by the other party. 
    // If the offer is accepted successfully, both parties will receive their tokens. 
    #[payable("*")]
    #[endpoint]
    fn accept_offer(
        &self,
        trade_offer_id: ManagedBuffer,
        #[payment_token] sent_token: TokenIdentifier,
        #[payment_amount] sent_amount: BigUint,
        #[payment_nonce] sent_nonce: u64,
        wanna_have_token : TokenIdentifier,
        wanna_have_amount : BigUint,
        wanna_have_nonce: u64
    ) -> SCResult<()> {
        require!(
            !self.trade_offer(&trade_offer_id).is_empty(),
            "An offer with this id does not exist."
        );

        let offer_info = self.trade_offer(&trade_offer_id).get();  
        require!(
            offer_info.token_identifier_offered == wanna_have_token && 
            offer_info.token_amount_offered == wanna_have_amount &&
            offer_info.token_nonce_offered == wanna_have_nonce,
            "Possible scam detected. The tokens you would get differ from the tokens you want"
        );
        require!(
            offer_info.token_identifier_wanted == sent_token && 
            offer_info.token_amount_wanted == sent_amount &&
            offer_info.token_nonce_wanted == sent_nonce,
            "Wrong token or not the correct number of tokens.."
        );

        self.trade_offer(&trade_offer_id).clear();
        self.finished_offer(&trade_offer_id).set(&true);

        // Exchanging tokens between parties
        let caller = self.blockchain().get_caller();
        self.send()
            .direct(&caller, &offer_info.token_identifier_offered, offer_info.token_nonce_offered, &offer_info.token_amount_offered, b"trade offer accepted");
        self.send()
            .direct(&offer_info.offer_creator, &sent_token, sent_nonce, &sent_amount, b"your trade offer has been accepted");

        Ok(())
    }

    // storage

    #[view(get_trade_offer)]
    #[storage_mapper("trade_offer")]
    fn trade_offer(&self, offer_id: &ManagedBuffer) -> SingleValueMapper<TradeOffer<Self::Api>>;

    #[view(get_finished_offer)]
    #[storage_mapper("finished_offer")]
    fn finished_offer(&self, offer_id: &ManagedBuffer) -> SingleValueMapper<bool>;
}