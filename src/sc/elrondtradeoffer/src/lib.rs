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
    ) {
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
        #[payment_token] offer_token: EgldOrEsdtTokenIdentifier,
        #[payment_amount] offer_amount: BigUint,
        #[payment_nonce] offer_nonce: u64,
        trade_offer_id: ManagedBuffer,
        wanna_have_token : EgldOrEsdtTokenIdentifier,
        wanna_have_amount : BigUint,
        wanna_have_nonce: u64
    ) {
        require!(
            offer_amount > 0,
            "offer_amount needs to be greater than 0"
        );

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
            "An offer with this id was already existing"
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
    }
    
    // This endpoint cancels your trade offer and sends you back your tokens.
    #[endpoint]
    fn cancel_offer(
        &self,
        trade_offer_id: ManagedBuffer
    ) {
        require!(
            !self.trade_offer(&trade_offer_id).is_empty(),
            "An offer with this id does not exist"
        );

        let caller = self.blockchain().get_caller();
        let info = self.trade_offer(&trade_offer_id).get();  
        require!(
            info.offer_creator == caller,
            "You are not the creator"
        );
        
        self.trade_offer(&trade_offer_id).clear();
        self.finished_offer(&trade_offer_id).set(&2);

        self.send()
            .direct(&caller, &info.token_identifier_offered, info.token_nonce_offered, &info.token_amount_offered);
    }

    // This endpoint will be used by the other party. 
    // If the offer is accepted successfully, both parties will receive their tokens. 
    #[payable("*")]
    #[endpoint]
    fn accept_offer(
        &self,
        trade_offer_id: ManagedBuffer,
        #[payment_token] sent_token: EgldOrEsdtTokenIdentifier,
        #[payment_amount] sent_amount: BigUint,
        #[payment_nonce] sent_nonce: u64,
        wanna_have_token : EgldOrEsdtTokenIdentifier,
        wanna_have_amount : BigUint,
        wanna_have_nonce: u64
    ) {
        require!(
            !self.trade_offer(&trade_offer_id).is_empty(),
            "An offer with this id does not exist"
        );

        let offer_info = self.trade_offer(&trade_offer_id).get();  
        require!(
            offer_info.token_identifier_offered == wanna_have_token && 
            offer_info.token_amount_offered == wanna_have_amount &&
            offer_info.token_nonce_offered == wanna_have_nonce,
            "Tokens you would get differ from the tokens you want"
        );
        require!(
            offer_info.token_identifier_wanted == sent_token && 
            offer_info.token_amount_wanted == sent_amount &&
            offer_info.token_nonce_wanted == sent_nonce,
            "Wrong token or not the correct number of tokens"
        );

        self.trade_offer(&trade_offer_id).clear();
        self.finished_offer(&trade_offer_id).set(&1);

        // Exchanging tokens between parties
        let caller = self.blockchain().get_caller();
        self.send()
            .direct(&caller, &offer_info.token_identifier_offered, offer_info.token_nonce_offered, &offer_info.token_amount_offered);
        self.send()
            .direct(&offer_info.offer_creator, &sent_token, sent_nonce, &sent_amount);
    }

    // storage

    #[view(get_trade_offer)]
    #[storage_mapper("trade_offer")]
    fn trade_offer(&self, offer_id: &ManagedBuffer) -> SingleValueMapper<TradeOffer<Self::Api>>;

    #[view(are_offers_pending)]
    fn offers_pending(&self, offer_id_list: MultiValueEncoded<ManagedBuffer>) -> MultiValueEncoded<u8> {
        let mut result = MultiValueEncoded::new();
        for offer_id in offer_id_list.into_iter()
        {
            let not_found_offer = self.trade_offer(&offer_id).is_empty();
            if not_found_offer {
                result.push(0);
            }
            else {
                result.push(1);
            }
        }

        result
    }

    #[view(get_finished_offer)]
    #[storage_mapper("finished_offer")]
    fn finished_offer(&self, offer_id: &ManagedBuffer) -> SingleValueMapper<u8>;

    #[view(get_finished_offer_list)]
    fn finished_offer_list(&self, offer_id_list: MultiValueEncoded<ManagedBuffer>) -> MultiValueEncoded<u8> {
        let mut result = MultiValueEncoded::new();        
        for offer_id in offer_id_list.into_iter()
        {
            let offer_status = self.finished_offer(&offer_id).get();
            result.push(offer_status);
        }

        result
    }
}