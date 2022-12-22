import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CustomDataApiComponent } from './custom-data-api.component';

describe('CustomDataApiComponent', () => {
  let component: CustomDataApiComponent;
  let fixture: ComponentFixture<CustomDataApiComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CustomDataApiComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CustomDataApiComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
