create table if not exists customers (
    id uuid primary key,
    full_name text not null,
    email text not null,
    phone text not null,
    created_at_utc timestamptz not null,
    updated_at_utc timestamptz not null
);

create unique index if not exists ux_customers_email_phone on customers (email, phone);

create table if not exists orders (
    id uuid primary key,
    public_order_id text not null unique,
    created_at_utc timestamptz not null,
    updated_at_utc timestamptz not null,
    customer_id uuid not null references customers(id) on delete restrict,
    currency varchar(12) not null,
    payment_method varchar(32) not null,
    status varchar(32) not null,
    delivery_type varchar(32) not null,
    delivery_address text null,
    table_number text null,
    special_instructions text null,
    subtotal numeric(12,2) not null,
    tax_amount numeric(12,2) not null,
    service_charge numeric(12,2) not null,
    delivery_fee numeric(12,2) not null,
    total_amount numeric(12,2) not null
);

create index if not exists ix_orders_created_at on orders (created_at_utc desc);
create index if not exists ix_orders_status on orders (status);
create index if not exists ix_orders_customer_id on orders (customer_id);

create table if not exists order_items (
    id uuid primary key,
    order_id uuid not null references orders(id) on delete cascade,
    menu_item_id text not null,
    item_name text not null,
    unit_price numeric(12,2) not null,
    quantity integer not null,
    line_total numeric(12,2) not null
);

create index if not exists ix_order_items_order_id on order_items (order_id);

create table if not exists payments (
    order_id uuid primary key references orders(id) on delete cascade,
    provider varchar(32) null,
    status varchar(32) not null,
    provider_order_id text null,
    provider_payment_id text null,
    paid_at_utc timestamptz null,
    updated_at_utc timestamptz not null
);

create index if not exists ix_payments_status on payments (status);

create table if not exists order_status_history (
    id bigserial primary key,
    order_id uuid not null references orders(id) on delete cascade,
    status varchar(32) not null,
    note text null,
    changed_at_utc timestamptz not null
);

create index if not exists ix_order_status_history_order_id_changed_at on order_status_history (order_id, changed_at_utc desc);
