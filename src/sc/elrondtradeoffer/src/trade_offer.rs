use elrond_wasm::{
    api::ManagedTypeApi,
    types::{BigUint, ManagedAddress, TokenIdentifier},
};

elrond_wasm::derive_imports!();

#[derive(NestedEncode, NestedDecode, TopEncode, TopDecode, TypeAbi)]
pub struct TradeOffer<M: ManagedTypeApi> {
    pub offer_creator: ManagedAddress<M>,
    pub token_identifier_offered : TokenIdentifier<M>,
    pub token_amount_offered: BigUint<M>,
    pub token_nonce_offered: u64,
    pub token_identifier_wanted : TokenIdentifier<M>,
    pub token_amount_wanted: BigUint<M>,
    pub token_nonce_wanted: u64
}
